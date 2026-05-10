using UnityEngine;

public interface IRitualController
{
    void StartRitual();
    void EndRitual();
    void Interact(int stage); 
    bool IsRitualActive { get; }
    void AbortRitual();
}

