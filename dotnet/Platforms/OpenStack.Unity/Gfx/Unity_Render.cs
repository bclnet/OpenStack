using System.Collections.Generic;
using UnityEngine;
#pragma warning disable CS9113

namespace OpenStack.Gfx.Unity;

#region TestTriRenderer

/// <summary>
/// TestTriRenderer
/// </summary>
public class TestTriRenderer(IOpenGfx[] gfx, ISource source, object obj) : Renderer {
    readonly UnityGfxModel GfxModel = (UnityGfxModel)gfx[GfX.XModel];
}

#endregion

#region ObjectRenderer

public class ObjectRenderer(IOpenGfx[] gfx, ISource source, object obj) : Renderer {
    readonly UnityGfxModel GfxModel = (UnityGfxModel)gfx[GfX.XModel];
    readonly object Obj = obj;

    public override void Start() {
        GfxModel.ObjectManager.CreateObject(source, Obj, true, null).Wait();
    }
}

#endregion

#region TextureRenderer

/// <summary>
/// TextureRenderer
/// </summary>
public class TextureRenderer(IOpenGfx[] gfx, ISource source, object obj) : Renderer {
    readonly UnityGfxModel GfxModel = (UnityGfxModel)gfx[GfX.XModel];
    readonly object Obj = obj;

    public override void Start() {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Plane); obj.isStatic = true; obj.name = "Texture";
        obj.transform.rotation = Quaternion.Euler(-90f, -180f, 180f);
        var meshRenderer = obj.GetComponent<MeshRenderer>();
        (meshRenderer.material, _) = GfxModel.MaterialManager.CreateMaterial(source, new MaterialStdProp { Textures = new Dictionary<string, object> { ["Main"] = Obj } }).Result;

        // cursor
        //var tex = GfxModel.TextureManager.CreateTexture(Obj).tex;
        //Cursor.SetCursor(tex, Vector2.zero, CursorMode.Auto);
    }
}

#endregion

#region EngineRenderer

public class EngineRenderer(IOpenGfx[] gfx, ISource source, object obj) : Renderer {
    UnityOpenEngine Engine;
    //GameObject PlayerPrefab = GameObject.Find("Player0");

    public override void Dispose() { base.Dispose(); Engine?.Dispose(); }

    public override void Start() {
        //Log.Info($"PlayerPrefab: {PlayerPrefab}");
        var db = (ICellDatabase)obj;
        Engine = new UnityOpenEngine(queue => new CellManager(db.Query, queue, new UnityCellBuilder(db.Archive, db.Query, gfx)), false);
        Engine.SpawnPlayer(db).Wait();
    }

    public override void Update(float deltaTime) => Engine?.Update();
}

#endregion
