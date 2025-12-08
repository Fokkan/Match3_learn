using UnityEngine;

[CreateAssetMenu(fileName = "StageDatabase", menuName = "Match3/StageDatabase")]
public class StageDatabase : ScriptableObject
{
    public StageData[] stages;
}
