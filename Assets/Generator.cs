using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.UIElements;

public enum State {
    INIT,
    RUNNING,
    FINISHED
}

public class Cell {
    public Vector3Int pos;
    public float survival;
    public float age;

    public Cell(Vector3Int pos, float survialChance) {
        this.pos = pos;
        this.survival = survialChance;
    }
}

public class Generator : MonoBehaviour {
    public TileBase[] tilesBlock;
    private Tilemap tilemap;

    private byte[,] state;

    public Vector3Int bottomLeft;
    public Vector3Int topRight;
    private int width;
    private int height;

    private State runState = State.INIT;

    private float zoom;
    public float zoomSpeed;
    public float zoomMultiplier;
    public float zoomMax;

    public float infection;
    public float[] survialChance;
    public float survialChanceRate;

    public int points;
    public int pointsConverted;
    public int extraCost;
    public int available;
    
    private List<Cell> growing = new List<Cell>();

    public Text counterText;
    public Text availableText;
    public GameObject gameOverText;
    
    // Start is called before the first frame update
    void Start() {
        tilemap = GetComponentInChildren<Tilemap>();

        zoom = Camera.main.orthographicSize;

        width = topRight.x - bottomLeft.x;
        height = topRight.y - bottomLeft.y;

        state = new byte[width, height];

        var minRadius = (width / 2 - 2) * (width / 2 - 2);
        var maxRadius = (width / 2) * (width / 2);

        for (int _x = 0; _x < width; _x++) {
            for (int _y = 0; _y < height; _y++) {
                var x = _x - width / 2;
                var y = _y - height / 2;

                var r = x * x + y * y;
                if (r < minRadius) {
                } else if (r < maxRadius) {
                    state[_x, _y] = (byte) (tilesBlock.Length - 2);
                } else {
                    state[_x, _y] = (byte) (tilesBlock.Length - 1);
                }
            }
        }

//        for (int x = -10; x < width + 10; x++) {
//            for (int y = -10; y < height + 10; y++) {
//                if (x < 0 || y < 0 || x >= width || y >= height) {
//                    tilemap.SetTile(new Vector3Int(x + bottomLeft.x, y + bottomLeft.y, 0), tilesBlock[tilesBlock.Length - 1]);
//                }
//            }
//        }

        var center = tilemap.CellToWorld(Vector3Int.zero);
        Camera.main.transform.position = center + new Vector3(0, 0, -10);

        infection = 0;

        refreshTiles();
        
    }

    // Update is called once per frame
    void Update() {
        if (runState != State.FINISHED) {
            var worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = tilemap.WorldToCell(worldPoint) + topRight;
            
            if (Input.GetMouseButton(0)) {
                if (available > 0) {
                    infection = Mathf.Clamp(infection * 1.15f, 0.1f, survialChance.Length - 1);
                    kickoff((int) infection, cell);
                }
//            } else if (Input.GetMouseButton(1)) {    // attack
//                kickoff(survialChance.Length, cell);
            } 
        }

        availableText.text = available + " x";
        counterText.text = points + "";
        
        Camera.main.orthographicSize = Mathf.SmoothStep(Camera.main.orthographicSize, zoom, Time.deltaTime * zoomSpeed);
    }

    private void refreshTiles() {
        bool[,] growingCells = new bool[width, height];
        foreach (var cell in growing) {
            growingCells[cell.pos.x, cell.pos.y] = true;
        }

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                var position = new Vector3Int(x + bottomLeft.x, y + bottomLeft.y, 0);

                var tile = tilesBlock[state[x, y]];
                if (tile is AnimatedTile) {
                    var speed = 2;
                    ((AnimatedTile) tile).m_MinSpeed = speed;
                    ((AnimatedTile) tile).m_MaxSpeed = speed;
                }

                tilemap.SetTile(position, tile);

                if ( growingCells[x, y]) {
                    tilemap.SetColor(position, Color.yellow);
                } else {
                    tilemap.SetColor(position, Color.white);
                }

            }
        }
    }

    public IEnumerator grow() {
        while (runState != State.FINISHED) {
            spawn();
            yield return new WaitForSeconds(Random.Range(0.7f, 0.7f));            
        }
    }

    public void spawn() {
        List<Cell> newGrowing = new List<Cell>();

        var oldPoints = points;
           
        foreach (var cell in growing) {
            var x = Random.Range(-1, 2);
            var y = Random.Range(-1, 2);

            var _x = cell.pos.x + x;
            var _y = cell.pos.y + y;

            var infectionType = state[cell.pos.x, cell.pos.y];

            cell.survival = Mathf.Clamp(cell.survival + survialChanceRate, 0, 10);
            
            if (_x >= 0 && _y >= 0 && _x < width && _y < height) {
                if (state[_x, _y] < infectionType) {
                    state[_x, _y] = infectionType;

                    if (infectionType != survialChance.Length + 1) points += infectionType + 1;

                    if (Random.Range(0f, 1f) < cell.survival) {
                        newGrowing.Add(new Cell(new Vector3Int(_x, _y, 0), cell.survival));
                    }
                }
            }

            if (Random.Range(0f, cell.age) < cell.survival) {
                cell.age++;
                newGrowing.Add(cell);
                
            }
        }

        if (points == oldPoints && available == 0) {
            gameOverText.SetActive(true);
        }
        
        var cost = extraCost * ((int) infection + 1);
        var extra = (points - pointsConverted) / cost;
        var costs = extra * cost;
        pointsConverted += costs;
        available += extra;
        
        growing = newGrowing;

        if (zoom < zoomMax) {
            zoom *= zoomMultiplier;
        }

        refreshTiles();
    }

    public void kickoff(int infection, Vector3Int cell ) {
        if (cell.x < width && cell.y < height &&
            cell.x >= 0 && cell.y >= 0 && state[cell.x, cell.y] < infection + 1) {
            state[cell.x, cell.y] = (byte) (infection + 1);

            if (infection < survialChance.Length) {
                growing.Add(new Cell(cell, survialChance[infection]));
                available--;
            } else {
                growing.Add(new Cell(cell, 4));
            }

            refreshTiles();
    
            if (runState == State.INIT) {
                runState = State.RUNNING;
                StartCoroutine(grow());
                StartCoroutine(startEnemy());
            }
        }
    }
    
    public IEnumerator startEnemy() {
        while (runState != State.FINISHED) {
            yield return new WaitForSeconds(Random.Range(4f, 5f));
            var pos = new Vector3Int(Random.Range(-7, 8) + width / 2, Random.Range(-7, 8) + height / 2, 0);
            kickoff(survialChance.Length, pos);    // deadly
        }
    }

    public void restart() {
        SceneManager.LoadScene(0);
    }

}