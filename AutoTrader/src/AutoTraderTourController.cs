using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace AutoTrader
{
    internal class AutoTraderTourController
    {
        private const float PostTradeLeaveDelay = 1.5f;

        private readonly AutoTraderLogic _autoTraderLogic;
        private readonly HashSet<Settlement> _recentlyVisitedTowns;

        private Settlement _currentTarget;
        private Settlement _lastVisitedTown;
        private bool _isTourActive;
        private bool _isTradingAtCurrentSettlement;
        private float _postTradeTimer;

        public AutoTraderTourController(AutoTraderLogic autoTraderLogic)
        {
            _autoTraderLogic = autoTraderLogic;
            _recentlyVisitedTowns = new HashSet<Settlement>();
        }

        public void Tick(float dt)
        {
            if (Game.Current == null)
            {
                return;
            }

            GameState activeState = Game.Current.GameStateManager?.ActiveState;
            if (activeState is not MapState)
            {
                return;
            }

            HandleToggleInput();

            if (!_isTourActive)
            {
                return;
            }

            if (MobileParty.MainParty == null)
            {
                CancelTourWithMessage(new TextObject("{=ATAutoTourInterrupted}Auto-trade tour interrupted.", null));
                return;
            }

            if (MobileParty.MainParty.MapEvent != null || MobileParty.MainParty.IsDisbanding)
            {
                CancelTourWithMessage(new TextObject("{=ATAutoTourInterrupted}Auto-trade tour interrupted.", null));
                return;
            }

            if (MobileParty.MainParty.CurrentSettlement != null)
            {
                HandleSettlementArrival(dt);
            }
            else
            {
                _isTradingAtCurrentSettlement = false;
                EnsurePartyIsMoving();
            }
        }

        private void HandleToggleInput()
        {
            if (_autoTraderLogic.IsTradingActive)
            {
                return;
            }

            if (Input.IsKeyDown(InputKey.LeftAlt)
                && Input.IsKeyDown(InputKey.A)
                && Input.IsKeyPressed(InputKey.R)
                && Game.Current.GameStateManager?.ActiveState is MapState)
            {
                if (_isTourActive)
                {
                    CancelTourWithMessage(new TextObject("{=ATAutoTourStopped}Auto-trade tour stopped.", null));
                }
                else
                {
                    StartTour();
                }
            }
        }

        private void StartTour()
        {
            if (MobileParty.MainParty == null)
            {
                CancelTourWithMessage(new TextObject("{=ATAutoTourInterrupted}Auto-trade tour interrupted.", null));
                return;
            }

            _recentlyVisitedTowns.Clear();
            _currentTarget = null;
            _lastVisitedTown = MobileParty.MainParty?.CurrentSettlement;
            _postTradeTimer = 0f;
            _isTradingAtCurrentSettlement = false;
            _isTourActive = true;
            AutoTraderHelpers.PrintMessage(new TextObject("{=ATAutoTourStarted}Auto-trade tour started. Press <Alt + A + R> to stop.", null).ToString());
            EnsurePartyIsMoving();
        }

        private void HandleSettlementArrival(float dt)
        {
            Settlement settlement = MobileParty.MainParty.CurrentSettlement;
            if (settlement == null)
            {
                return;
            }

            if (!settlement.IsTown)
            {
                if (!_isTradingAtCurrentSettlement)
                {
                    _currentTarget = null;
                    LeaveSettlementIfPossible();
                }
                return;
            }

            _lastVisitedTown = settlement;

            if (!_recentlyVisitedTowns.Contains(settlement))
            {
                _recentlyVisitedTowns.Add(settlement);
            }

            if (!_isTradingAtCurrentSettlement && !_autoTraderLogic.IsTradingActive)
            {
                _isTradingAtCurrentSettlement = true;
                _postTradeTimer = 0f;
                _autoTraderLogic.PerformAutoTrade(false);
            }
            else if (_isTradingAtCurrentSettlement && !_autoTraderLogic.IsTradingActive)
            {
                _postTradeTimer += dt;
                if (_postTradeTimer >= PostTradeLeaveDelay)
                {
                    LeaveSettlementIfPossible();
                    _currentTarget = null;
                    _isTradingAtCurrentSettlement = false;
                    EnsurePartyIsMoving();
                }
            }
        }

        private void EnsurePartyIsMoving()
        {
            if (!_isTourActive || MobileParty.MainParty == null || _autoTraderLogic.IsTradingActive)
            {
                return;
            }

            if (_currentTarget == null || _currentTarget == MobileParty.MainParty.CurrentSettlement)
            {
                _currentTarget = FindNextTarget(false);
                if (_currentTarget == null)
                {
                    Settlement preservedLast = _lastVisitedTown;
                    _recentlyVisitedTowns.Clear();
                    if (preservedLast != null)
                    {
                        _recentlyVisitedTowns.Add(preservedLast);
                    }
                    _currentTarget = FindNextTarget(true);
                    if (_currentTarget == null)
                    {
                        CancelTourWithMessage(new TextObject("{=ATAutoTourNoTarget}Unable to find a new town for the auto-trade tour.", null));
                        return;
                    }
                }
            }

            Settlement currentMoveTarget = GetCurrentMoveTarget();
            if (currentMoveTarget != _currentTarget)
            {
                StartMovingToSettlement(_currentTarget);
            }
        }

        private Settlement FindNextTarget(bool allowVisitedReuse)
        {
            if (MobileParty.MainParty == null)
            {
                return null;
            }

            Settlement bestSettlement = null;
            float bestDistance = float.MaxValue;

            foreach (Town town in Town.AllTowns)
            {
                Settlement candidate = town?.Settlement;
                if (candidate == null || !candidate.IsTown)
                {
                    continue;
                }

                if (candidate.IsUnderSiege)
                {
                    continue;
                }

                if (!allowVisitedReuse && _recentlyVisitedTowns.Contains(candidate))
                {
                    continue;
                }

                if (candidate == MobileParty.MainParty.CurrentSettlement)
                {
                    continue;
                }

                float estimatedLandRatio;
                float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, candidate, false, MobileParty.NavigationType.Default, out estimatedLandRatio);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSettlement = candidate;
                }
            }

            return bestSettlement;
        }

        private Settlement GetCurrentMoveTarget()
        {
            if (MobileParty.MainParty == null)
            {
                return null;
            }

            try
            {
                PropertyInfo targetProperty = typeof(MobileParty).GetProperty("TargetSettlement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (targetProperty != null)
                {
                    return targetProperty.GetValue(MobileParty.MainParty) as Settlement;
                }
            }
            catch (Exception e)
            {
                AutoTraderHelpers.PrintDebugMessage("Failed to read current move target: " + e.ToString());
            }

            return null;
        }

        private void StartMovingToSettlement(Settlement settlement)
        {
            if (settlement == null || MobileParty.MainParty == null)
            {
                return;
            }

            try
            {
                var ai = MobileParty.MainParty.Ai;
                if (ai != null)
                {
                    MethodInfo moveMethod = ai.GetType().GetMethod("SetMoveGoToSettlement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (moveMethod != null)
                    {
                        moveMethod.Invoke(ai, new object[] { settlement });
                        return;
                    }
                }

                MethodInfo fallbackMove = typeof(MobileParty).GetMethod("SetMoveGoToSettlement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                fallbackMove?.Invoke(MobileParty.MainParty, new object[] { settlement });
            }
            catch (Exception e)
            {
                AutoTraderHelpers.PrintDebugMessage("Failed to command party to move to settlement: " + e.ToString());
            }
        }

        private void LeaveSettlementIfPossible()
        {
            if (MobileParty.MainParty?.CurrentSettlement == null)
            {
                return;
            }

            try
            {
                PlayerEncounter.LeaveEncounter = true;
            }
            catch (Exception e)
            {
                AutoTraderHelpers.PrintDebugMessage("Failed to flag leave encounter: " + e.ToString());
            }

            try
            {
                LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
            }
            catch (Exception e)
            {
                AutoTraderHelpers.PrintDebugMessage("Failed to leave settlement automatically: " + e.ToString());
            }
        }

        private void CancelTourWithMessage(TextObject textObject)
        {
            if (textObject != null)
            {
                AutoTraderHelpers.PrintMessage(textObject.ToString());
            }

            _isTourActive = false;
            _currentTarget = null;
            _isTradingAtCurrentSettlement = false;
            _postTradeTimer = 0f;
            _recentlyVisitedTowns.Clear();
            _lastVisitedTown = null;
        }
    }
}
