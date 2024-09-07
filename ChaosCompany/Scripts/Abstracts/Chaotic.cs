using Unity.Netcode;

namespace ChaosCompany.Scripts.Abstracts;

public abstract class Chaotic
{
    public RoundManager RoundManager { get; private set; }
    public NetworkObject? NetworkObject { get; protected set; }
    public bool ItsJoever { get; protected set; }

    public Chaotic(RoundManager roundManager)
    {
        RoundManager = roundManager;
    }

    public virtual void Update()
    {

    }

    public virtual Chaotic? Spawn()
    {
        return null;
    }
}