using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

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

        var radius = (width / 2 - 2) * (width / 2 - 2);
        
        for (int _x = 0; _x < width; _x++) {
            for (int _y = 0; _y < height; _y++) {
                var x = _x - width / 2;
                var y = _y - height / 2;
                
                if (x * x + y * y > radius) {
                    state[_x, _y] = (byte) (tilesBlock.Length - 1);
                }
            }
        }

        var center = tilemap.CellToWorld(Vector3Int.zero);
        Camera.main.transform.position = center + new Vector3(0, 0, -10);
    }

    // Update is called once per frame
    void Update() {
        if (runState != State.FINISHED && Input.GetMouseButton(0)) {
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
            yield return new WaitForSeconds(Random.Range(0.4f, 1));
        }
    }

    public void spawn() {
        List<Vector3Int> newGrowing = new List<Vector3Int>();

        foreach (var cell in growing) {
            var x = Random.Range(-1, 2);
            var y = Random.Range(-1, 2);
            
            var _x = cell.x + x;
            var _y = cell.y + y;

            var s = state[cell.x, cell.y];
            
            if (_x >= 0 && _y >= 0 && _x < width && _y < height && state[_x, _y] < s) {
                state[_x, _y] = s;
                newGrowing.Add(new Vector3Int(_x, _y, 0));
            }
        }

        growing.AddRange(newGrowing);

        if (Camera.main.orthographicSize < 20) {
            Camera.main.orthographicSize *= 1.03f;
        }
    }

    public void kickoff() {
        var worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = tilemap.WorldToCell(worldPoint) + topRight;

        Debug.Log(cell);
        
        if (cell.x < width && cell.y < height && 
            cell.x >= 0 && cell.y >= 0) {
            
            growing.Add(cell);
            state[cell.x, cell.y] = randomTile();

            if (runState == State.INIT) {
                runState = State.RUNNING;
                StartCoroutine(grow());
            }

        }
        
    }

    private byte randomTile() {
        return (byte) Random.Range(1, tilesBlock.Length);
    }
}