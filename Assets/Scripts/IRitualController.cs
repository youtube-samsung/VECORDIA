using UnityEngine;
using System;

public interface IRitualController
{
    void StartRitual();
    void EndRitual();
    void Interact(int stage);
    bool IsRitualActive { get; }
    void AbortRitual();

    //event Action OnInterruptionRequested;
    //void PauseRitual();
    //void ResumeRitual();
}

