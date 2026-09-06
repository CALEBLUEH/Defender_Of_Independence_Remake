using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    public GameObject player; // 拖入 Player 物体
    public SimplePlayerControl fpsController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = 0f; // 确保游戏时间正常运行
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);

            fpsController = player.GetComponent<SimplePlayerControl>();

        // 一开场先解锁鼠标，让玩家能点按钮
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

     public void backToMain()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void StartGame()
    {
        Time.timeScale = 1f; // 恢复游戏时间
        mainMenuPanel.SetActive(false);

         fpsController.LockCursor();
    }

}
