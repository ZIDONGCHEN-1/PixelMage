using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SkinColorChanger : MonoBehaviour
{
    public SpriteRenderer playerRenderer;
    public Button[] colorButtons;
    public Color[] colors;

    private const string COLOR_PREF_KEY = "PlayerSkinColorIndex";

    void Start()
    {
        colors = new Color[]
{
    new Color(1.0f, 0.8f, 0.6f, 1.0f),
    new Color(0.87f, 0.68f, 0.48f, 1.0f),
    new Color(0.4f, 0.26f, 0.13f, 1.0f),
    new Color(1.0f, 0.6f, 0.7f, 1.0f),
    new Color(0.5f, 1.0f, 0.5f, 1.0f),
    new Color(0.6f, 0.8f, 1.0f, 1.0f),
    new Color(0.8f, 0.6f, 1.0f, 1.0f),
    new Color(0.7f, 0.7f, 0.7f, 1.0f),
    new Color(0.6f, 1.0f, 0.8f, 1.0f),
    new Color(1.0f, 0.9f, 0.5f, 1.0f)
};


        // 设置按钮点击事件
        for (int i = 0; i < colorButtons.Length; i++)
        {
            int index = i;
            colorButtons[i].onClick.AddListener(() => ChangeColor(index));

            Image img = colorButtons[i].GetComponent<Image>();
            if (img != null)
            {
                img.color = colors[i];
            }
        }

        // 尝试读取保存的颜色索引
        int savedIndex = PlayerPrefs.GetInt(COLOR_PREF_KEY, 0); // 默认为0
        ApplyColor(savedIndex);
    }

    public void ChangeColor(int index)
    {
        ApplyColor(index);

        // 储存颜色索引
        PlayerPrefs.SetInt(COLOR_PREF_KEY, index);
        PlayerPrefs.Save();
    }

    private void ApplyColor(int index)
    {
        if (index >= 0 && index < colors.Length)
        {
            playerRenderer.color = colors[index];
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
