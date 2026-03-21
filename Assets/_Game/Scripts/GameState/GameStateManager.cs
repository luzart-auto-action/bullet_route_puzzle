using System.Collections.Generic;
using UnityEngine;
using BulletRoute.Core;

namespace BulletRoute.GameState
{
    public interface IGameState
    {
        GameStateType StateType { get; }
        void Enter();
        void Update();
        void Exit();
    }

    public class GameStateManager : MonoBehaviour
    {
        private Dictionary<GameStateType, IGameState> _states = new Dictionary<GameStateType, IGameState>();
        private IGameState _currentState;

        public GameStateType CurrentStateType => _currentState?.StateType ?? GameStateType.Loading;

        private void Awake()
        {
            ServiceLocator.Register(this);

            // Register states
            RegisterState(new MainMenuState());
            RegisterState(new LoadingState());
            RegisterState(new SetupState());
            RegisterState(new SimulatingState());
            RegisterState(new WinState());
            RegisterState(new FailState());
            RegisterState(new PausedState());
        }

        public void RegisterState(IGameState state)
        {
            _states[state.StateType] = state;
        }

        public void ChangeState(GameStateType newState)
        {
            if (_currentState != null && _currentState.StateType == newState) return;

            var previousType = _currentState?.StateType ?? GameStateType.Loading;
            _currentState?.Exit();

            if (_states.TryGetValue(newState, out var state))
            {
                _currentState = state;
                _currentState.Enter();

                EventBus.Publish(new GameStateChangedEvent
                {
                    PreviousState = previousType,
                    NewState = newState
                });
            }
        }

        private void Update()
        {
            _currentState?.Update();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameStateManager>();
        }
    }

    // === State Implementations ===
    public class MainMenuState : IGameState
    {
        public GameStateType StateType => GameStateType.MainMenu;
        public void Enter()
        {
            EventBus.Publish(new ShowPanelEvent { PanelName = "MainMenu" });
        }
        public void Update() { }
        public void Exit()
        {
            EventBus.Publish(new HidePanelEvent { PanelName = "MainMenu" });
        }
    }

    public class LoadingState : IGameState
    {
        public GameStateType StateType => GameStateType.Loading;
        public void Enter() { }
        public void Update() { }
        public void Exit() { }
    }

    public class SetupState : IGameState
    {
        public GameStateType StateType => GameStateType.Setup;
        public void Enter()
        {
            // Player can interact with tiles
        }
        public void Update() { }
        public void Exit() { }
    }

    public class SimulatingState : IGameState
    {
        public GameStateType StateType => GameStateType.Simulating;
        public void Enter()
        {
            // Disable input during simulation
        }
        public void Update() { }
        public void Exit() { }
    }

    public class WinState : IGameState
    {
        public GameStateType StateType => GameStateType.Win;
        public void Enter()
        {
            EventBus.Publish(new ShowPanelEvent { PanelName = "WinPanel" });
        }
        public void Update() { }
        public void Exit()
        {
            EventBus.Publish(new HidePanelEvent { PanelName = "WinPanel" });
        }
    }

    public class FailState : IGameState
    {
        public GameStateType StateType => GameStateType.Fail;
        public void Enter()
        {
            EventBus.Publish(new ShowPanelEvent { PanelName = "FailPanel" });
        }
        public void Update() { }
        public void Exit()
        {
            EventBus.Publish(new HidePanelEvent { PanelName = "FailPanel" });
        }
    }

    public class PausedState : IGameState
    {
        public GameStateType StateType => GameStateType.Paused;
        public void Enter()
        {
            Time.timeScale = 0f;
        }
        public void Update() { }
        public void Exit()
        {
            Time.timeScale = 1f;
        }
    }
}
