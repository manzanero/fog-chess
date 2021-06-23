using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public Transform tilesParent;
    public Transform piecesParent;
    public Transform blackCapturedParent;
    public Transform whiteCapturedParent;

    public static string Color = "White";

    public Tile tileOver;
    public Piece pieceOver;
    public Piece pieceDragged;
    public Piece pieceSelected;
    public Vector3 mousePosition;
    public readonly Tile[,] Tiles = new Tile[8, 8];
    public Piece[] pieces = new Piece[32];
    public Piece[] whitePieces = new Piece[16];
    public Piece[] blackPieces = new Piece[16];
    public Tile[] blackCapturedTiles = new Tile[16];
    public Tile[] whiteCapturedTiles = new Tile[16];
    public bool kingDisrupted;

    private Tile _tileOrigin;
    private List<Tile> _cachedTilesOutlined = new List<Tile>();
    private List<Tile> _tilesRevealed = new List<Tile>();

    private static Camera _mainCamera;
    private static int _tileLayerMask;
    public static GameManager Instance;

    private void Awake()
    {
        Instance = this;

        _mainCamera = Camera.main;
        _tileLayerMask = LayerMask.GetMask("Tiles");

        for (var i = 0; i < blackCapturedParent.childCount; i++)
        {
            var tile = blackCapturedParent.GetChild(i).GetComponent<Tile>();
            tile.capture = true;
            tile.captureSide = "b";
            tile.order = i;
            blackCapturedTiles[i] = tile;
        }

        for (var i = 0; i < whiteCapturedParent.childCount; i++)
        {
            var tile = whiteCapturedParent.GetChild(i).GetComponent<Tile>();
            tile.capture = true;
            tile.captureSide = "w";
            tile.order = i;
            whiteCapturedTiles[i] = tile;
        }

        for (var i = 0; i < tilesParent.childCount; i++)
        {
            var tile = tilesParent.GetChild(i).GetComponent<Tile>();
            tile.x = i % 8;
            tile.y = i / 8;
            Tiles[tile.x, tile.y] = tile;
            _tilesRevealed.Add(tile);
        }

        for (var i = 0; i < piecesParent.childCount; i++)
        {
            var piece = piecesParent.GetChild(i).GetComponent<Piece>();
            var piecePosition = piece.transform.position;
            var tile = Tiles[Mathf.RoundToInt(piecePosition.x), Mathf.RoundToInt(piecePosition.z)];
            piece.tile = tile;
            tile.piece = piece;
            pieces[i] = piece;
            if (i < 16) whitePieces[i] = piece;
            else blackPieces[i - 16] = piece;
        }
    }

    private IEnumerable<Piece> MyPieces => Color == "White" ? whitePieces : blackPieces;

    public static bool MouseOverUi { get; private set; }
    public static GameObject CurrentSelectedGameObject { get; private set; }

    private void UpdateMouse()
    {
        var current = EventSystem.current;
        MouseOverUi = current.IsPointerOverGameObject();
        CurrentSelectedGameObject = current.currentSelectedGameObject;

        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var rayCastHit, 300f, _tileLayerMask))
        {
            tileOver = rayCastHit.transform.GetComponent<Tile>();
            mousePosition = rayCastHit.point;
        }
        else tileOver = null;
    }

    public void AddToCaptured(Piece piece)
    {
        if (piece.color == "White") whiteCapturedTiles.FirstOrDefault(x => !x.piece)?.PlacePiece(piece);
        else blackCapturedTiles.FirstOrDefault(x => !x.piece)?.PlacePiece(piece);

        piece.tile.Show();
    }

    private void UpdatePiece()
    {
        if (pieceOver && !pieceOver.MouseOver) pieceOver = null;
        if (pieceDragged && !pieceDragged.Dragged) pieceDragged = null;

        if (pieceDragged && pieceDragged.IsMyColor)
        {
            pieceSelected = pieceDragged;
            var pieceSelectedTransform = pieceSelected.transform;
            var dest = new Vector3(Mathf.RoundToInt(mousePosition.x), 0.125f, Mathf.RoundToInt(mousePosition.z));
            var orig = pieceSelectedTransform.position;
            var dir = dest - orig;
            pieceSelectedTransform.position = orig + 10 * Time.deltaTime * dir;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (pieceSelected) pieceSelected.tile.PlacePiece(pieceSelected);
            if (!tileOver) return;

            if (!pieceSelected && pieceOver)
                pieceSelected = pieceOver;
            else if (pieceSelected && pieceOver && pieceSelected == pieceOver)
                pieceSelected = null;
            else if (pieceSelected && !pieceSelected.IsMyColor)
                pieceSelected = pieceOver;
            else if (pieceSelected && pieceSelected.IsMyColor && tileOver.piece && tileOver.piece.IsMyColor)
                pieceSelected = pieceOver;
            else if (pieceSelected && pieceSelected.IsMyColor && tileOver.piece && !tileOver.piece.IsMyColor)
                CapturePiece(pieceSelected.tile, tileOver, pieceSelected);
            else if (pieceSelected && pieceSelected.IsMyColor && !tileOver.piece)
                MovePiece(pieceSelected.tile, tileOver, pieceSelected);
        }
    }

    private void CapturePiece(Tile initTiles, Tile destTile, Piece piece = null)
    {
        piece ??= initTiles.piece;
        Debug.Log($"{initTiles.name} ({piece.color} {piece.type}) captured " +
                  $"{destTile.name} ({destTile.piece.color} {destTile.piece.type})");
        AddToCaptured(destTile.piece);
        destTile.PlacePiece(piece);
        pieceSelected = null;

        var d1 = initTiles.capture ? destTile.captureSide : destTile.x.ToString();
        var d2 = initTiles.capture ? destTile.order.ToString() : destTile.y.ToString();

        ServerManager.MessagesToSend.Add(
            new ServerManager.Msg {topic = "actions", payload = new List<string> {"capture", piece.name, d1, d2}});
    }

    private void MovePiece(Tile initTiles, Tile destTile, Piece piece = null)
    {
        piece ??= initTiles.piece;
        Debug.Log($"{initTiles.name} ({piece.color} {piece.type}) moved to {destTile.name}");
        destTile.PlacePiece(piece);
        pieceSelected = null;

        var d1 = initTiles.capture ? destTile.captureSide : destTile.x.ToString();
        var d2 = initTiles.capture ? destTile.order.ToString() : destTile.y.ToString();

        ServerManager.MessagesToSend.Add(
            new ServerManager.Msg {topic = "actions", payload = new List<string> {"move", piece.name, d1, d2}});
    }

    private void EndTurn()
    {
        ServerManager.MessagesToSend.Add(
            new ServerManager.Msg {topic = "actions", payload = new List<string> {"end_turn"}});
    }

    private void UpdateOutlines()
    {
        if (_cachedTilesOutlined.Any())
            foreach (var tile in _cachedTilesOutlined)
                tile.outline.enabled = false;

        if (pieceSelected)
        {
            var tilesInSight = pieceSelected.RefreshTilesInSight();

            foreach (var tile in tilesInSight)
            {
                tile.outline.color = pieceSelected.IsMyColor ? 0 : 2;
                tile.outline.enabled = true;
            }

            _cachedTilesOutlined = tilesInSight;
        }
        else
        {
            _cachedTilesOutlined.Clear();
        }
    }

    private void UpdateVision()
    {
        var tilesWithPiece = MyPieces.Select(x => x.tile).Where(x => !x.capture);
        var combined = MyPieces
            .Select(x => x.RefreshTilesInSight())
            .Aggregate((acc, list) => acc.Union(list).ToList());
        var tilesToReveal = tilesWithPiece.Union(combined).ToList();

        foreach (var tile in _tilesRevealed.Except(tilesToReveal)) tile.Hide();
        foreach (var tile in tilesToReveal.Except(_tilesRevealed)) tile.Show();

        _tilesRevealed = tilesToReveal.ToList();
    }

    private void Update()
    {
        UpdateMouse();
        UpdatePiece();
        UpdateOutlines();
        UpdateVision();
    }

    #region Utils

    public static T FindResource<T>(string path)
    {
        var res = FindResources<T>(path);
        if (!res.Any()) throw new Exception($"Resource not found: {path}");
        return res[0];
    }

    public static List<T> FindResources<T>(string path) => Resources.LoadAll(path, typeof(T)).Cast<T>().ToList();

    public static string NewId() => Guid.NewGuid().ToString().Substring(0, 8);

    public static string ToBase64(string data) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(data));

    public static string FromBase64(string data) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(data));

    public static string NowIsoDate()
    {
        var localTime = DateTime.Now;
        var localTimeAndOffset = new DateTimeOffset(localTime, TimeZoneInfo.Local.GetUtcOffset(localTime));
        var str = localTimeAndOffset.ToString("O");
        return str.Substring(0, 26) + str.Substring(27);
    }

    #endregion
}