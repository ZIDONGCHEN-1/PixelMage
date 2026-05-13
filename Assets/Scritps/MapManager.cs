using UnityEngine;

public class MapManager : MonoBehaviour
{
    public Transform player;                 // 玩家或摄像机
    public Transform[] backgrounds;          // 背景图数组（两个或更多）
    public float backgroundWidth = 20f;      // 每张背景的宽度（根据实际设置）

    void Update()
    {
        foreach (var bg in backgrounds)
        {
            float distance = player.position.x - bg.position.x;

            // 向右滚动
            if (distance > backgroundWidth)
            {
                float maxX = GetFarthestX();
                bg.position = new Vector3(maxX + backgroundWidth, bg.position.y, bg.position.z);
            }
            // 向左滚动
            else if (distance < -backgroundWidth)
            {
                float minX = GetNearestX();
                bg.position = new Vector3(minX - backgroundWidth, bg.position.y, bg.position.z);
            }
        }
    }

    // 获取最右边的背景图位置
    float GetFarthestX()
    {
        float maxX = backgrounds[0].position.x;
        for (int i = 1; i < backgrounds.Length; i++)
        {
            if (backgrounds[i].position.x > maxX)
                maxX = backgrounds[i].position.x;
        }
        return maxX;
    }

    // 获取最左边的背景图位置
    float GetNearestX()
    {
        float minX = backgrounds[0].position.x;
        for (int i = 1; i < backgrounds.Length; i++)
        {
            if (backgrounds[i].position.x < minX)
                minX = backgrounds[i].position.x;
        }
        return minX;
    }
}
