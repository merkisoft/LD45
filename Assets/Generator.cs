using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum State {
    INIT,
    RUNNING,
    FINISHED
}

public class Generator : MonoBehaviour {
    public Tile[] tilesBlock;
    private Tilemap tilemap;

    private byte[,] state;

    public Vector3Int bottomLeft;
    public Vector3Int topRight;
    private int width;
    private int height;

    private State runState = State.INIT;

    private List<Vector3Int> growing = new List<Vector3Int>();

    // Start is called before the first frame update
    void Start() {
        tilemap = GetComponentInChildren<Tilemap>();

        width = topRight.x - bottomLeft.x;
        height = topRight.y - bottomLeft.y;

        state = new byte[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                state[x, y] = 0;
            }
        }

        var center = tilemap.CellToWorld(Vector3Int.zero);
        Camera.main.transform.position = center + new Vector3(0, 0, -10);
    }

    // Update is called once per frame
    void Update() {
        if (runState != State.FINISHED && Input.GetMouseButtonDown(0)) {
            kickoff();
        }

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                tilemap.SetTile(new Vector3Int(x + bottomLeft.x, y + bottomLeft.y, 0), tilesBlock[state[x, y]]);
            }
        }
    }

    public IEnumerator grow() {
        while (runState != State.FINISHED) {
            spawn();
            yield return new WaitForSeconds(Random.Range(0.1f, 1));
        }
    }

    public void spawn() {
        var cell = growing[Random.Range(0, growing.Count)];

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                var _x = cell.x + x;
                var _y = cell.y + y;
                if (state[_x, _y] == 0 && Random.Range(0, 1f) < 0.05f) {
                    state[_x, _y] = state[cell.x, cell.y];
                    growing.Add(new Vector3Int(_x, _y, 0));
                }
            }
        }
    }

    public void kickoff() {
        var worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = tilemap.WorldToCell(worldPoint) + topRight;
        
        growing.Add(cell);
        state[cell.x, cell.y] = randomTile();

        if (runState == State.INIT) {
            runState = State.RUNNING;
            StartCoroutine(grow());
        }


        Camera.main.orthographicSize *= 1.03f;
    }

    private byte randomTile() {
        return (byte) Random.Range(1, tilesBlock.Length);
    }
}