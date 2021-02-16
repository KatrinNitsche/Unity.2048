using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public static int gridWidth = 4, gridHeight = 4;
    public static Transform[,] grid = new Transform[gridWidth, gridHeight];
    public Canvas gameOverCanvas;
    public Text gameScoreText;
    public Text bestScoreText;
    public AudioClip moveTilesSound;
    public AudioClip mergeTilesSound;
        
    private int score = 0;
    private int numberOfCoroutinesRunning = 0;
    private bool generatedNewTileThisTurn = true;
    private AudioSource audioSource;
    private static NotATile[,] previousGrid = new NotATile[gridWidth, gridHeight];
    private static NotATile[,] saveGrid = new NotATile[gridWidth, gridHeight];
    private int previousScore = 0;
    private int savedScore = 0;
    private bool madeFirstMove = false;
    private bool savedGame = false;

    private void Start()
    {
        audioSource = transform.GetComponent<AudioSource>();
        GenerateNewTile(2);
        UpdateBestScore();       
    }

    private void Update()
    {
        if (numberOfCoroutinesRunning != 0) return;

        if (!generatedNewTileThisTurn)
        {
            generatedNewTileThisTurn = true;
            GenerateNewTile(1);
        }

        if (!CheckGameOver())
        {
            CheckUserInput();
        }
        else
        {
            SaveBestScore();
            UpdateBestScore();
            gameOverCanvas.gameObject.SetActive(true);
        }
    }

    private void CheckUserInput()
    {
        bool down = Input.GetKeyDown(KeyCode.DownArrow);
        bool up = Input.GetKeyDown(KeyCode.UpArrow);
        bool left = Input.GetKeyDown(KeyCode.LeftArrow);
        bool right = Input.GetKeyDown(KeyCode.RightArrow);

        if (down || up || left || right)
        {
            if (!madeFirstMove) madeFirstMove = true;
            StorePreviousTiles();

            PrepareTileForMerging();

            if (down) { MoveAllTiles(Vector2.down); }

            if (up) { MoveAllTiles(Vector2.up); }

            if (left) { MoveAllTiles(Vector2.left); }

            if (right) { MoveAllTiles(Vector2.right); }
        }
    }

    private void UpdateBestScore()
    {
        bestScoreText.text = PlayerPrefs.GetInt("bestscore").ToString();
    }

    private void SaveBestScore()
    {
        int oldBestScore = PlayerPrefs.GetInt("bestscore");

        if (oldBestScore < score)
        {
            PlayerPrefs.SetInt("bestscore", score);
            PlayerPrefs.Save();
        }
    }

    private void UpdateScore()
    {
        gameScoreText.text = score.ToString("00000000");
    }

    private bool CheckGameOver()
    {
        if (transform.childCount < gridWidth * gridHeight) return false;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Transform currentTile = grid[x, y];
                if (currentTile == null) continue;

                Transform tileBelow = null;
                Transform tileBeside = null;

                if (y != 0) tileBelow = grid[x, y - 1];
                if (x != gridWidth - 1) tileBeside = grid[x + 1, y];

                if (tileBeside != null)
                    if (currentTile.GetComponent<Tile>().tileValue == tileBeside.GetComponent<Tile>().tileValue) return false;

                if (tileBelow != null)
                    if (currentTile.GetComponent<Tile>().tileValue == tileBelow.GetComponent<Tile>().tileValue) return false;
            }
        }

        return true;
    }

    private void MoveAllTiles(Vector2 direction)
    {
        int tilesMovedCount = 0;
        UpdateGrid();

        if (direction == Vector2.left)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        if (MoveTile(grid[x, y], direction))
                            tilesMovedCount++;
                    }
                }
            }
        }

        if (direction == Vector2.right)
        {
            for (int x = gridWidth - 1; x >= 0; x--)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        if (MoveTile(grid[x, y], direction))
                            tilesMovedCount++;
                    }
                }
            }
        }

        if (direction == Vector2.down)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        if (MoveTile(grid[x, y], direction))
                            tilesMovedCount++;
                    }
                }
            }
        }

        if (direction == Vector2.up)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = gridHeight - 1; y >= 0; y--)
                {
                    if (grid[x, y] != null)
                    {
                        if (MoveTile(grid[x, y], direction))
                            tilesMovedCount++;
                    }
                }
            }
        }

        if (tilesMovedCount != 0)
        {
            audioSource.PlayOneShot(moveTilesSound);
            generatedNewTileThisTurn = false;
        }

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (grid[x, y] != null)
                {
                    Transform transform = grid[x, y];
                    StartCoroutine(SlideTile(transform.gameObject, 10f));
                }
            }
        }
    }

    private bool MoveTile(Transform tile, Vector2 direction)
    {
        Vector2 startPositon = tile.localPosition;
        Vector2 phantomTilePosition = tile.localPosition;

        tile.GetComponent<Tile>().startingPosition = startPositon;

        while (true)
        {
            Vector2 previousPosition = phantomTilePosition;
            phantomTilePosition += direction;

            if (CheckIsInsideGrid(phantomTilePosition))
            {
                if (CheckIsAtValidPosition(phantomTilePosition))
                {
                    tile.GetComponent<Tile>().moveToPosition = phantomTilePosition;
                    grid[(int)previousPosition.x, (int)previousPosition.y] = null;
                    grid[(int)phantomTilePosition.x, (int)phantomTilePosition.y] = tile;
                }
                else
                {
                    if (!CheckAndCombineTiles(tile, phantomTilePosition, previousPosition))
                    {
                        phantomTilePosition += -direction;
                        tile.GetComponent<Tile>().moveToPosition = phantomTilePosition;

                        if (phantomTilePosition == startPositon)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                phantomTilePosition += -direction;
                tile.GetComponent<Tile>().moveToPosition = phantomTilePosition;

                if (phantomTilePosition == startPositon)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }

    private bool CheckAndCombineTiles(Transform movingTile, Vector2 phantomTilePosition, Vector2 previousPosition)
    {
        Vector2 position = movingTile.transform.localPosition;
        Transform collidingTile = grid[(int)phantomTilePosition.x, (int)phantomTilePosition.y];

        int movingTileValue = movingTile.GetComponent<Tile>().tileValue;
        int collidingTileValue = collidingTile.GetComponent<Tile>().tileValue;

        if (movingTileValue == collidingTileValue && !movingTile.GetComponent<Tile>().mergedThisTurn &&
            !collidingTile.GetComponent<Tile>().mergedThisTurn && !collidingTile.GetComponent<Tile>().willMergeWidthCollidingTile)
        {
            movingTile.GetComponent<Tile>().destroyMe = true;
            movingTile.GetComponent<Tile>().colliderTile = collidingTile;
            movingTile.GetComponent<Tile>().moveToPosition = phantomTilePosition;

            grid[(int)previousPosition.x, (int)previousPosition.y] = null;
            grid[(int)phantomTilePosition.x, (int)phantomTilePosition.y] = movingTile;
            movingTile.GetComponent<Tile>().willMergeWidthCollidingTile = true;

            UpdateScore();
            return true;
        }

        return false;
    }

    private void GenerateNewTile(int howMany)
    {
        for (int i = 0; i < howMany; i++)
        {
            var locationForNewTile = GetRandomLocationForNewTile();

            string tile = "tile_2";
            float chanceOfTwo = Random.Range(0f, 1f);
            if (chanceOfTwo > 0.9) tile = "tile_4";

            GameObject newTile = (GameObject)Instantiate(Resources.Load(tile, typeof(GameObject)), locationForNewTile, Quaternion.identity);
            newTile.transform.parent = transform;

            grid[(int)newTile.transform.localPosition.x, (int)newTile.transform.localRotation.y] = newTile.transform;
            newTile.transform.localScale = new Vector2(0, 0);
            newTile.transform.localPosition = new Vector2(newTile.transform.localPosition.x + 0.5f, newTile.transform.localPosition.y + 0.5f);
            StartCoroutine(
                NewTilePopIn(newTile,
                new Vector2(0, 0),
                new Vector2(1, 1),
                10f,
                newTile.transform.localPosition,
                new Vector2(newTile.transform.localPosition.x - 0.5f, newTile.transform.localPosition.y - 0.5f)));
        }
    }

    private void UpdateGrid()
    {
        // remove all not blank fields
        for (int x = 0; x < gridHeight; x++)
        {
            for (int y = 0; y < gridWidth; y++)
            {
                if (grid[x, y] != null)
                {
                    if (grid[x, y].parent == transform)
                    {
                        grid[x, y] = null;
                    }
                }
            }
        }

        // add all tiles to grid
        foreach (Transform tile in transform)
        {
            Vector2 v = new Vector2(Mathf.Round(tile.position.x), Mathf.Round(tile.position.y));
            grid[(int)v.x, (int)v.y] = tile;
        }
    }

    private Vector2 GetRandomLocationForNewTile()
    {
        List<int> x = new List<int>();
        List<int> y = new List<int>();

        for (int j = 0; j < gridWidth; j++)
        {
            for (int i = 0; i < gridHeight; i++)
            {
                if (grid[j, i] == null)
                {
                    x.Add(j);
                    y.Add(i);
                }
            }
        }

        int randomIndex = Random.Range(0, x.Count);
        int randX = x.ElementAt(randomIndex);
        int randY = y.ElementAt(randomIndex);

        return new Vector2(randX, randY);
    }

    private bool CheckIsInsideGrid(Vector2 position)
    {
        if (position.x >= 0 && position.x <= gridWidth - 1 && position.y >= 0 && position.y <= gridHeight - 1)
            return true;

        return false;
    }

    private bool CheckIsAtValidPosition(Vector2 position)
    {
        if (grid[(int)position.x, (int)position.y] == null) return true;

        return false;
    }

    private void PrepareTileForMerging()
    {
        foreach (Transform transform in transform)
        {
            transform.GetComponent<Tile>().mergedThisTurn = false;
        }
    }

    /// <summary>
    /// Restarts the game by reseting the grid
    /// </summary>
    public void PlayAgain()
    {
        grid = new Transform[gridWidth, gridHeight];
        score = 0;

        List<GameObject> children = new List<GameObject>();
        foreach (Transform t in transform)
        {
            children.Add(t.gameObject);
        }

        children.ForEach(t => DestroyImmediate(t));

        gameOverCanvas.gameObject.SetActive(false);
        GenerateNewTile(2);
        UpdateScore();
        StorePreviousTiles();
    }

    #region Undo

    public void Undo()
    {
        if (!madeFirstMove) return;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = null;

                NotATile notATile = previousGrid[x, y];
                if (notATile != null)
                {
                    int tileValue = notATile.Value;
                    string newTileName = $"tile_{tileValue}";
                    GameObject newTile = (GameObject)Instantiate(Resources.Load(newTileName, typeof(GameObject)), notATile.Location, Quaternion.identity);
                    newTile.transform.parent = transform;

                    grid[x, y] = newTile.transform;
                }
            }
        }

        score = previousScore;
        UpdateScore();
    }

    private void StorePreviousTiles()
    {
        previousScore = score;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                previousGrid[x, y] = null;
                Transform tempTile = grid[x, y];
                if (tempTile != null)
                {
                    NotATile notATile = new NotATile() { Location = tempTile.localPosition, Value = tempTile.GetComponent<Tile>().tileValue };
                    previousGrid[x, y] = notATile;
                }
            }
        }
    }

    #endregion

    private IEnumerator NewTilePopIn(GameObject tile, Vector2 initialScale,
        Vector2 finalScale, float timeScale, Vector2 initalPosition, Vector2 finalPosition)
    {
        numberOfCoroutinesRunning++;

        float progress = 0;
        while (progress <= 1)
        {
            tile.transform.localScale = Vector2.Lerp(initialScale, finalScale, progress);
            tile.transform.localPosition = Vector2.Lerp(initalPosition, finalPosition, progress);
            progress += Time.deltaTime * timeScale;
            yield return null;
        }

        tile.transform.localScale = finalScale;
        tile.transform.localPosition = finalPosition;

        numberOfCoroutinesRunning--;
    }

    private IEnumerator SlideTile(GameObject tile, float timeScale)
    {
        numberOfCoroutinesRunning++;

        float progress = 0;
        while (progress <= 1)
        {
            tile.transform.localPosition = Vector2.Lerp(tile.GetComponent<Tile>().startingPosition, tile.GetComponent<Tile>().moveToPosition, progress);
            progress += Time.deltaTime * timeScale;
            yield return null;
        }

        tile.transform.localPosition = tile.GetComponent<Tile>().moveToPosition;

        if (tile.GetComponent<Tile>().destroyMe)
        {
            int movingTileValue = tile.GetComponent<Tile>().tileValue;
            if (tile.GetComponent<Tile>().colliderTile != null)
            {
                DestroyImmediate(tile.GetComponent<Tile>().colliderTile.gameObject);
            }

            Destroy(tile.gameObject);
            string newTileName = $"tile_{movingTileValue * 2}";
            score += movingTileValue * 2;
            UpdateScore();

            audioSource.PlayOneShot(mergeTilesSound);

            GameObject newTile = (GameObject)Instantiate(Resources.Load(newTileName, typeof(GameObject)), tile.transform.localPosition, Quaternion.identity);
            newTile.transform.parent = transform;
            newTile.GetComponent<Tile>().mergedThisTurn = true;
            grid[(int)newTile.transform.localPosition.x, (int)newTile.transform.localPosition.y] = newTile.transform;

            newTile.transform.localScale = new Vector2(0, 0);
            newTile.transform.localPosition = new Vector2(newTile.transform.localPosition.x + 0.5f, newTile.transform.localPosition.y + 0.5f);

            yield return StartCoroutine(
                NewTilePopIn(newTile,
                new Vector2(0, 0),
                new Vector2(1, 1),
                10f,
                newTile.transform.localPosition,
                new Vector2(newTile.transform.localPosition.x - 0.5f, newTile.transform.localPosition.y - 0.5f)));
        }

        numberOfCoroutinesRunning--;
    }

    public void ExitGame()
    {
        SaveBestScore();
        Application.Quit();
    }

    #region Save & Load

    public void Save()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                saveGrid[x, y] = null;

                if (grid[x,y] != null)            
                {
                    Transform t = grid[x, y];
                    int value = t.GetComponent<Tile>().tileValue;
                    Vector2 location = t.localPosition;
                    NotATile notATile = new NotATile() { Location = location, Value = value };

                    saveGrid[x, y] = notATile;
                }
            }
        }

        savedScore = score;
        savedGame = true;
    }

    public void Load()
    {
        if (!savedGame) return;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = null;

                NotATile notATile = saveGrid[x, y];
                if (notATile != null)
                {
                    int tileValue = notATile.Value;
                    string newTileName = $"tile_{tileValue}";
                    GameObject newTile = (GameObject)Instantiate(Resources.Load(newTileName, typeof(GameObject)), notATile.Location, Quaternion.identity);
                    newTile.transform.parent = transform;

                    grid[x, y] = newTile.transform;
                }
            }
        }

        score = savedScore;
        UpdateScore();
    }

    #endregion
}