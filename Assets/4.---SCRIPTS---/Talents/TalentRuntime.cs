[System.Serializable]
public class TalentRuntime
{
    public TalentData data;
    public int currentPoints;

    public TalentRuntime(TalentData data)
    {
        this.data = data;
        currentPoints = 0;
    }
}
