using Unity.Netcode;
using Timer = ChaosCompany.Scripts.Components.Timer;

namespace ChaosCompany.Scripts.ChaoticThings;

public abstract class Chaotic
{
    public RoundManager RoundManager { get; private set; }
    public NetworkObject? NetworkObject { get; protected set; }
    public bool ItsJoever { get; protected set; }

    protected Timer changeType;

    public Chaotic(RoundManager roundManager, Timer changeType)
    {
        RoundManager = roundManager;
        this.changeType = changeType;
    }

    public abstract Chaotic? Spawn();
}