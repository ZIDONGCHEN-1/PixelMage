using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    public TextMeshPro dialogueText;
    public GameObject dialoguePanel;
    public float typingSpeed = 0.05f;

    private string[] dialogueLines = new string[]
    {
        "法师，你终于来了。",
        "我们守到最后，但还是挡不住黑暗。",
        "地牢的大门已经开启……试炼，等着你。",
        "走吧，把我们的失败，变成你的胜利。"
    };

    private int currentLineIndex = 0;
    private bool isPlayerInRange = false;
    private bool isTyping = false;
    private bool hasTalked = false; // 防止重复触发

    private void Start()
    {
        dialoguePanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTalked) return;

        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            other.GetComponent<PlayerController>().canMove = false;
            StartDialogue();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            dialoguePanel.SetActive(false);
            StopAllCoroutines();
        }
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            if (!isTyping)
            {
                ShowNextLine();
            }
        }
    }

    void StartDialogue()
    {
        dialoguePanel.SetActive(true);
        currentLineIndex = 0;
        StartCoroutine(TypeLine(dialogueLines[currentLineIndex]));
    }

    void ShowNextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < dialogueLines.Length)
        {
            StartCoroutine(TypeLine(dialogueLines[currentLineIndex]));
        }
        else
        {
            dialoguePanel.SetActive(false);
            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            playerController.canMove = true;
            hasTalked = true; // 标记为已对话
        }
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in line.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }
}
