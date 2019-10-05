using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
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

    private float zoom;
    public float zoomSpeed;
    public float zoomMultiplier;
    public float zoomMax;

    public byte infection;
    public float survialChance;
    public float survialChanceRate;

    private List<Vector3Int> growing = new List<Vector3Int>();

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
                if (r > minRadius && r < maxRadius) {
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

        infection = 1;
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

        Camera.main.orthographicSize = Mathf.SmoothStep(Camera.main.orthographicSize, zoom, Time.deltaTime * zoomSpeed);
    }

    public IEnumerator grow() {
        while (runState != State.FINISHED) {
            spawn();
            yield return new WaitForSeconds(Random.Range(0.4f, 1));

            survialChance = Mathf.Clamp(survialChance + survialChanceRate, 0, 10);
        }
    }

    public void spawn() {
        List<Vector3Int> newGrowing = new List<Vector3Int>();

        var generation = new Vector3Int(0, 0, 1);

        foreach (var cell in growing) {
            var x = Random.Range(-1, 2);
            var y = Random.Range(-1, 2);

            var _x = cell.x + x;
            var _y = cell.y + y;

            var s = state[cell.x, cell.y];

            if (_x >= 0 && _y >= 0 && _x < width && _y < height && state[_x, _y] < s) {
                state[_x, _y] = s;
            }

            if (Random.Range(0f, 1f) < survialChance) {
                newGrowing.Add(new Vector3Int(_x, _y, 0));
            }

            if (Random.Range(0f, cell.z) < survialChance) {
                newGrowing.Add(cell + generation);
            }
        }

        growing = newGrowing;

        if (zoom < zoomMax) {
            zoom *= zoomMultiplier;
        }
    }

    public void kickoff() {
        var worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = tilemap.WorldToCell(worldPoint) + topRight;

        if (cell.x < width && cell.y < height &&
            cell.x >= 0 && cell.y >= 0 && state[cell.x, cell.y] < infection) {
            growing.Add(cell);
            state[cell.x, cell.y] = infection;

            if (runState == State.INIT) {
                runState = State.RUNNING;
                StartCoroutine(grow());
            }
        }
    }

    private byte randomTile() {
        return (byte) Random.Range(1, tilesBlock.Length - 1);
    }
}