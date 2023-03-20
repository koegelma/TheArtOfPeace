using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameState gameState;

    public List<Enemy> enemies = new List<Enemy>();
    public int enemiesCreated = 0;
    public int enemiesToSpawn = 3;
    private int enemiesDefeated = 0;

    private void Awake()
    {
        instance = this;
    }

    public int AddEnemy(Enemy enemy)
    {
        enemies.Add(enemy);
        enemiesCreated++;
        return enemiesCreated;
    }

    public void RemoveEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
        enemiesDefeated++;
        if (enemiesDefeated >= enemiesToSpawn)
        {
            WinGame();
        }
    }

    public void StartGame()
    {
        gameState = GameState.Playing;
        Actions.OnGameStarted?.Invoke();
    }

    public void EndGame()
    {
        gameState = GameState.GameOver;
        Actions.OnGameLost?.Invoke();
    }

    public void WinGame()
    {
        gameState = GameState.Win;
        Actions.OnGameWon?.Invoke();
    }

    public void PauseGame()
    {
        gameState = GameState.Paused;
        Actions.OnGamePaused?.Invoke();
    }

    public void ResumeGame()
    {
        gameState = GameState.Playing;
        Actions.OnGameResumed?.Invoke();
    }

    public void LoadScene(string _name)
    {
        Debug.Log("Changing scene to " + _name);
        SceneManager.LoadScene(_name);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game");
        Application.Quit();
    }
}
