using System.Collections.Generic;
using UnityEngine;
#pragma warning disable CS9113

namespace OpenStack.Gfx.Unity;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer(IOpenGfx[] gfx, object obj) : Renderer {
    readonly UnityGfxModel GfxModel = (UnityGfxModel)gfx[GfX.XModel];
}

#endregion

#region ObjectRenderer

public class ObjectRenderer(IOpenGfx[] gfx, object obj) : Renderer {
    readonly UnityGfxModel GfxModel = (UnityGfxModel)gfx[GfX.XModel];
    readonly object Obj = obj;

    public override void Start() {
        GfxModel.ObjectManager.CreateObject(Obj, null);
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer(IOpenGfx[] gfx, object obj) : Renderer {
    readonly UnityGfxModel GfxModel = (UnityGfxModel)gfx[GfX.XModel];
    readonly object Obj = obj;

    public override void Start() {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Plane); obj.isStatic = true; obj.name = "Texture";
        obj.transform.rotation = Quaternion.Euler(-90f, -180f, 180f);
        var meshRenderer = obj.GetComponent<MeshRenderer>();
        (meshRenderer.material, _) = GfxModel.MaterialManager.CreateMaterial(new MaterialStdProp { Textures = new Dictionary<string, object> { ["Main"] = Obj } });

        // cursor
        //var tex = GfxModel.TextureManager.CreateTexture(Obj).tex;
        //Cursor.SetCursor(tex, Vector2.zero, CursorMode.Auto);
    }
}

#endregion

#region EngineRenderer

public class EngineRenderer(IOpenGfx[] gfx, object obj) : Renderer {
    IOpenGfx[] Gfx = gfx;
    readonly ICellDatabase Db = obj as ICellDatabase;
    UnityOpenEngine Engine;
    //GameObject PlayerPrefab = GameObject.Find("Player0");

    public override void Dispose() { base.Dispose(); Engine?.Dispose(); }

    public override void Start() {
        //Log.Info($"PlayerPrefab: {PlayerPrefab}");
        var arc = (ISourceWithPlatform)Db.Archive;
        Gfx = arc.Gfx;
        var query = Db.Query;
        Engine = new UnityOpenEngine(queue => new CellManager(query, queue, new UnityCellBuilder(query, Gfx)), false);
        Engine.SpawnPlayer(Db);
    }

    public override void Update(float deltaTime) => Engine?.Update();
}

#endregion
