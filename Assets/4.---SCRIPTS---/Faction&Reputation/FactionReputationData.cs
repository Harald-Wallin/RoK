using UnityEngine;

[System.Serializable]
public class FactionReputationData
{

    public Faction faction;

    public int level = 1;
    public int currentXP = 0;

    public bool discovered = false;
    public bool tracked = false;
    public bool murderEnabled = false;

}

