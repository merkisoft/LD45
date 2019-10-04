using System.Collections;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum State {
    INIT, RUNNING, FINISHED
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
    void Update () {
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
            yield return new WaitForSeconds(Random.Range(1, 3));
        }
    }

    public void spawn() {
//        state[0, 0] = (byte) Random.Range(1, tilesBlock.Length);
    }

    public void kickoff() {
        var worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var cell = tilemap.WorldToCell(worldPoint) + topRight;
        Debug.Log(cell);
        
        state[cell.x, cell.y] = (byte) Random.Range(1, tilesBlock.Length);

        if (runState == State.INIT) {
            runState = State.RUNNING;
            StartCoroutine(grow());
        }

    }
    
}