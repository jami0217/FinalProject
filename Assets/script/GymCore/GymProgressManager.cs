using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinalProject.Gym.Core
{
    [DisallowMultipleComponent]
    public sealed class GymProgressManager : MonoBehaviour
    {
        [SerializeField] private GymStationDefinition[] stations = GymStationDefinition.Defaults();
        private GymProgressModel model;
        public event Action ProgressChanged;
        public event Action AllStationsCompleted;
        public event Action RoundReset;
        public int CompletedCount => Model.CompletedCount;
        public int TotalCount => Model.TotalCount;
        public bool AllCompleted => Model.AllCompleted;
        public IReadOnlyList<GymStationDefinition> Stations => Model.Stations;
        private GymProgressModel Model { get { if (model == null) Initialize(stations); return model; } }

        private void Initialize(GymStationDefinition[] definitions)
        {
            try { model = new GymProgressModel(definitions); }
            catch (ArgumentException error)
            {
                Debug.LogWarning(error.Message + " Using five default stations for this Play session; fix the Inspector list.", this);
                model = new GymProgressModel(GymStationDefinition.Defaults());
            }
        }

        public bool ConfigureStations(GymStationDefinition[] definitions)
        {
            GymProgressModel replacement;
            try { replacement = new GymProgressModel(definitions); }
            catch (ArgumentException error) { Debug.LogWarning(error.Message, this); return false; }
            if (Model.HasAnyProgress)
            {
                for (int i = 0; i < TotalCount; i++)
                    if (Model.Stations[i].id != replacement.Stations[i].id)
                    { Debug.LogWarning("Station IDs cannot change during an active round. Existing progress was kept.", this); return false; }
                return true;
            }
            stations = definitions;
            model = replacement;
            ProgressChanged?.Invoke();
            return true;
        }

        public GymStationState GetState(string id) => Model.GetState(id);
        public bool IsCompleted(string id) => Model.Contains(id) && Model.GetState(id) == GymStationState.Completed;
        public bool StartStation(string id)
        {
            GymStationState previous = Model.GetState(id);
            if (!Model.StartStation(id)) { Debug.LogWarning("Unregistered station ID: " + (id ?? "<null>"), this); return false; }
            if (previous != Model.GetState(id)) ProgressChanged?.Invoke();
            return true;
        }
        public bool CompleteStation(string id)
        {
            if (!Model.CompleteStation(id)) return false;
            ProgressChanged?.Invoke();
            if (AllCompleted) AllStationsCompleted?.Invoke();
            return true;
        }
        public void ResetRound()
        {
            Model.Reset();
            RoundReset?.Invoke();
            ProgressChanged?.Invoke();
        }
    }
}
