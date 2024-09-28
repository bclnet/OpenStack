import math, numpy as np
from enum import Enum, Flag
from openstk.gfx.util import _throw, _np_normalize, _np_getTranslation4x4, _np_createScale4x4, _np_createLookAt4x4, _np_createPerspectiveFieldOfView4x4

CAMERASPEED = 300.
PiOver2 = 1.570796
FOV = 0.7853982 # MathX.PiOver4

# typedefs
class IMaterial: pass
class IModel: pass

# forwards
class AABB: pass
class Camera: pass
class IVBIB: pass
class Attribute: pass
class Frustum: pass
class RenderSlotType: pass

# Shader
class Shader:
    _getUniformLocation: callable
    _getAttribLocation: callable
    _uniforms: dict[str, int] = {}
    name: str
    program: int
    parameters: dict[str, bool]
    renderModes: list[str]

    def __init__(self, getUniformLocation: callable, getAttribLocation: callable, name: str = None, program: int = None, parameters: dict[str, bool] = None, renderModes: list[str] = None):
        self._getUniformLocation = getUniformLocation or _throw('Null')
        self._getAttribLocation = getAttribLocation or _throw('Null')
        self.name = name
        self.program = program
        self.parameters = parameters
        self.renderModes = renderModes
    
    def getUniformLocation(self, name: str) -> int:
        if name in self._uniforms: return self._uniforms[name]
        value = self._getUniformLocation(self.program, name); self._uniforms[name] = value; return value

    def getAttribLocation(self, name: str) -> int:
        return self._getAttribLocation(self.program, name)

# IPickingTexture
class IPickingTexture:
    isActive: bool
    debug: bool
    shader: Shader
    debugShader: Shader
    def render() -> None: pass
    def resize(width: int, height: int) -> None: pass
    def finish() -> None: pass

# OnDiskBufferData
class OnDiskBufferData:
    elementCount: int
    elementSizeInBytes : int # stride for vertices. Type for indices
    attributes: list[Attribute] # Vertex attribs. Empty for index buffers
    data: bytes

    class RenderSlotType(Enum):
        RENDER_SLOT_INVALID = -1
        RENDER_SLOT_PER_VERTEX = 0
        RENDER_SLOT_PER_INSTANCE = 1

    class Attribute:
        semanticName: str
        semanticIndex: int
        format: int
        offset: int
        slot: int
        slotType: RenderSlotType
        instanceStepRate: int

# IVBIB
class IVBIB:
    vertexBuffers: list[OnDiskBufferData]
    indexBuffers: list[OnDiskBufferData]
    def remapBoneIndices(self, remapTable: list[int]) -> IVBIB: pass

# AABB
class AABB:
    min: np.ndarray
    max: np.ndarray
    @property
    def size(self) -> np.ndarray: return self.max - self.min
    @property
    def center(self) -> np.ndarray: return (self.min + self.max) * 0.5
    def __str__(self): return f'AABB [({self.min[0]},{self.min[1]},{self.min[2]}) -> ({self.max[0]},{self.max[1]},{self.max[2]}))'

    def __init__(self, *args):
        match args:
            case (min, max): self.min = min; self.max = max
            case (minX, minY, minZ, maxX, maxY, maxZ): self.min = np.array([minX, minY, minZ]); self.max = np.array([maxX, maxY, maxZ])
            case _: raise Exception(f'Unknown {args}')
    
    def contains(self, point: np.ndarray | AABB) -> bool:
        match point:
            case p if isinstance(point, np.ndarray):
                return p[0] >= self.min[0] and p[0] < self.max[0] and \
                    p[1] >= self.min[1] and p[1] < self.max[1] and \
                    p[2] >= self.min[2] and p[2] < self.max[2]
            case o if isinstance(point, AABB):
                return o.min[0] >= self.min[0] and o.max[0] <= self.max[0] and \
                    o.min[1] >= self.min[1] and o.max[1] <= self.max[1] and \
                    o.min[2] >= self.min[2] and o.max[2] <= self.max[2]
            case _: raise Exception(f'Unknown {point}')
    
    def intersects(self, other: AABB) -> bool:
        return other.max[0] >= self.min[0] and other.min[0] < self.max[0] and \
            other.max[1] >= self.min[1] and other.min[1] < self.max[1] and \
            other.max[2] >= self.min[2] and other.min[2] < self.max[2]
    
    def union(self, other: AABB) -> AABB:
        return AABB(np.min(self.min, other.min), np.max(self.max, other.max))
    
    def translate(self, offset: np.ndarray) -> AABB:
        return AABB(self.min + offset, self.max + offset)

    # Note: Since we're dealing with AABBs here, the resulting AABB is likely to be bigger than the original if rotation
    # and whatnot is involved. This problem compounds with multiple transformations. Therefore, endeavour to premultiply matrices
    # and only use this at the last step.
    def transform(self, transform: np.ndarray) -> AABB:
        points = [
            Vector4.Transform(Vector4(self.min[0], self.min[1], self.min[2], 1.), transform),
            Vector4.Transform(Vector4(self.max[0], self.min[1], self.min[2], 1.), transform),
            Vector4.Transform(Vector4(self.max[0], self.max[1], self.min[2], 1.), transform),
            Vector4.Transform(Vector4(self.min[0], self.max[1], self.min[2], 1.), transform),
            Vector4.Transform(Vector4(self.min[0], self.max[1], self.max[2], 1.), transform),
            Vector4.Transform(Vector4(self.min[0], self.min[1], self.max[2], 1.), transform),
            Vector4.Transform(Vector4(self.max[0], self.min[1], self.max[2], 1.), transform),
            Vector4.Transform(Vector4(self.max[0], self.max[1], self.max[2], 1.), transform)]
        min = points[0]
        max = points[0]
        for i in range(1, points.Length):
            min = np.min(min, points[i])
            max = np.max(max, points[i])
        return AABB(Vector3(min[0], min[1], min[2]), Vector3(max[0], max[1], max[2]))

# Frustum
class Frustum:
   planes: list[np.ndarray] = [None]*6

   @staticmethod
   def createEmpty() -> Frustum: f = Frustum; f.planes = []; return f

   def update(self, viewProjectionMatrix: np.ndarray) -> None:
      self.planes[0] = _np_normalize(np.array([
         viewProjectionMatrix[0,3] + viewProjectionMatrix[0,0],
         viewProjectionMatrix[1,3] + viewProjectionMatrix[1,0],
         viewProjectionMatrix[2,3] + viewProjectionMatrix[2,0],
         viewProjectionMatrix[3,3] + viewProjectionMatrix[3,0]]))
      self.planes[1] = _np_normalize(np.array([
         viewProjectionMatrix[0,3] - viewProjectionMatrix[0,0],
         viewProjectionMatrix[1,3] - viewProjectionMatrix[1,0],
         viewProjectionMatrix[2,3] - viewProjectionMatrix[2,0],
         viewProjectionMatrix[3,3] - viewProjectionMatrix[3,0]]))
      self.planes[2] = _np_normalize(np.array([
         viewProjectionMatrix[0,3] - viewProjectionMatrix[0,1],
         viewProjectionMatrix[1,3] - viewProjectionMatrix[1,1],
         viewProjectionMatrix[2,3] - viewProjectionMatrix[2,1],
         viewProjectionMatrix[3,3] - viewProjectionMatrix[3,1]]))
      self.planes[3] = _np_normalize(np.array([
         viewProjectionMatrix[0,3] + viewProjectionMatrix[0,1],
         viewProjectionMatrix[1,3] + viewProjectionMatrix[1,1],
         viewProjectionMatrix[2,3] + viewProjectionMatrix[2,1],
         viewProjectionMatrix[3,3] + viewProjectionMatrix[3,1]]))
      self.planes[4] = _np_normalize(np.array([
         viewProjectionMatrix[0,2],
         viewProjectionMatrix[1,2],
         viewProjectionMatrix[2,2],
         viewProjectionMatrix[3,2]]))
      self.planes[5] = _np_normalize(np.array([
         viewProjectionMatrix[0,3] - viewProjectionMatrix[0,2],
         viewProjectionMatrix[1,3] - viewProjectionMatrix[1,2],
         viewProjectionMatrix[2,3] - viewProjectionMatrix[2,2],
         viewProjectionMatrix[3,3] - viewProjectionMatrix[3,2]]))

   def clone(self) -> Frustum:
      r = Frustum()
      self.planes.copyTo(r.planes, 0)
      return r

   def intersects(self, box: AABB) -> bool:
      for i in range(self.planes.length):
         closest = np.array(
            box.min[0] if self.planes[i][0] < 0 else box.max[0],
            box.min[1] if self.planes[i][1] < 0 else box.max[1],
            box.min[2] if self.planes[i][2] < 0 else box.max[2])
         if Vector3.Dot(np.array(self.planes[i][0], self.planes[i][1], self.planes[i][2]), closest) + self.planes[i][3] < 0: return False
      return True

# Camera
class Camera:
    location: np.ndarray = np.ones(3)
    pitch: float = 0.
    yaw: float = 0.
    scale: float = 1.
    projectionMatrix: np.ndarray = np.zeros((4, 4))
    cameraViewMatrix: np.ndarray
    viewProjectionMatrix: np.ndarray
    viewFrustum: Frustum = Frustum()
    picker: IPickingTexture = None
    windowSize: np.ndarray = None
    aspectRatio: float = 0.

    def __init__(self):
        self.lookAt(np.zeros(3))

    def _recalculateMatrices(self) -> None:
        abc = _np_createScale4x4(self.scale)
        xyz = _np_createLookAt4x4(self.location, self.location + self._getForwardVector(), np.array([0., 0., 1.]))
        self.cameraViewMatrix = np.matmul(_np_createScale4x4(self.scale), _np_createLookAt4x4(self.location, self.location + self._getForwardVector(), np.array([0., 0., 1.])))
        self.viewProjectionMatrix = np.matmul(self.cameraViewMatrix, self.projectionMatrix)
        self.viewFrustum.update(self.viewProjectionMatrix)

    def _getForwardVector(self) -> np.ndarray: return np.array([(math.cos(self.yaw) * math.cos(self.pitch)), (math.sin(self.yaw) * math.cos(self.pitch)), math.sin(self.pitch)])

    def _getRightVector(self) -> np.ndarray: return np.array([math.cos(self.yaw - PiOver2), math.sin(self.yaw - PiOver2), 0.])

    def setViewport(self, x: int, y: int, width: int, height: int):
        # store window size and aspect ratio
        self.aspectRatio = width / height
        self.windowSize = np.array([width, height])
        # calculate projection matrix
        self.projectionMatrix = _np_createPerspectiveFieldOfView4x4(FOV, self.aspectRatio, 1., 40000.)
        self._recalculateMatrices()
        # setup viewport
        self.gfxViewport(x, y, width, height)
        if self.picker: self.picker.resize(width, height)

    def gfxViewport(self, x: int, y: int, width: int = 0, height: int = 0) -> None: pass

    def copyFrom(self, fromOther: Camera) -> None:
        self.aspectRatio = fromOther.aspectRatio
        self.windowSize = fromOther.windowSize
        self.location = fromOther.location
        self.pitch = fromOther.pitch
        self.yaw = fromOther.yaw
        self.projectionMatrix = fromOther.projectionMatrix
        self.cameraViewMatrix = fromOther.cameraViewMatrix
        self.viewProjectionMatrix = fromOther.viewProjectionMatrix
        self.viewFrustum.update(self.viewProjectionMatrix)
    
    def setLocation(self, location: np.ndarray) -> None:
        self.location = location
        self._recalculateMatrices()

    def setLocationPitchYaw(self, location: np.ndarray, pitch: float, yaw: float) -> None:
        self.location = location
        self.pitch = pitch
        self.yaw = yaw
        self._recalculateMatrices()

    def lookAt(self, target: np.ndarray) -> None:
        dir = _np_normalize(target - self.location)
        self.yaw = math.atan2(dir[1], dir[0])
        self.pitch = math.asin(dir[2])
        self._clampRotation()
        self._recalculateMatrices()

    def setFromTransformMatrix(self, matrix: np.ndarray) -> None:
        self.location = _np_getTranslation4x4(matrix)
        # extract view direction from view matrix and use it to calculate pitch and yaw
        dir = np.array([matrix[0, 0], matrix[0, 1], matrix[0, 2]])
        self.yaw = math.atan2(dir[1], dir[0])
        self.pitch = math.asin(dir[2])
        self._recalculateMatrices()

    def setScale(self, scale: float) -> None:
        self.scale = scale
        self._recalculateMatrices()

    def tick(self, deltaTime: int) -> None: pass

    # prevent camera from going upside-down
    def _clampRotation(self) -> None:
        if self.pitch >= PiOver2: self.pitch = PiOver2 - 0.001
        elif self.pitch <= -PiOver2: self.pitch = -PiOver2 + 0.001

# IMesh
class IMesh:
    data: dict[str, object]
    vbib: IVBIB
    minBounds: np.ndarray
    maxBounds: np.ndarray
    def getBounds(self) -> None: pass

# RenderMaterial
class RenderMaterial:
    material: IMaterial
    textures: dict[str, int] = {}
    isBlended: bool
    isToolsMaterial: bool
    alphaTestReference: float
    isAdditiveBlend: bool
    isRenderBackfaces: bool

    def __init__(self, material: IMaterial):
        self.material = material
        match material:
            case s if isinstance(material, IFixedMaterial): pass
            case p if isinstance(material, IParamMaterial):
                # TODO: Fixed with interface
                if 'F_ALPHA_TEST' in p.intParams and p.intParams['F_ALPHA_TEST'] == 1 and 'g_flAlphaTestReference' in p.floatParams: self.alphaTestReference = p.floatParams['g_flAlphaTestReference']
                self.isToolsMaterial = 'tools.toolsmaterial' in p.intAttributes
                self.isBlended = ('F_TRANSLUCENT' in p.intParams and p.IntParams['F_TRANSLUCENT'] == 1) or 'mapbuilder.water' in p.intAttributes or material.shaderName == 'vr_glass.vfx' or material.shaderName == 'tools_sprite.vfx'
                self.isAdditiveBlend = 'F_ADDITIVE_BLEND' in p.intParams and p.intParams['F_ADDITIVE_BLEND'] == 1
                self.isRenderBackfaces = 'F_RENDER_BACKFACES' in p.intParams and p.intParams['F_RENDER_BACKFACES'] == 1
            case _: raise Exception(f'Unknown {material}')
    def render(self, shader: Shader) -> None: pass
    def postRender(self) -> None: pass

# DrawCall
class DrawCall:
    primitiveType: int
    shader: Shader
    baseVertex: int
    # vertexCount: int
    startIndex: int
    indexCount: int
    # instanceIndex: int
    # instanceCount: int
    # uvDensity: float
    # flags: str
    tintColor: np.ndarray
    material: RenderMaterial
    vertexArrayObject: int
    vertexBuffer: (int, int)
    indexType: int
    indexBuffer: (int, int)

    class RenderMeshDrawPrimitiveFlags(Flag):
        None_ = 0x0
        UseShadowFastPath = 0x1
        UseCompressedNormalTangent = 0x2
        IsOccluder = 0x4
        InputLayoutIsNotMatchedToMaterial = 0x8
        HasBakedLightingFromVertexStream = 0x10
        HasBakedLightingFromLightmap = 0x20
        CanBatchWithDynamicShaderConstants = 0x40
        DrawLast = 0x80
        HasPerInstanceBakedLightingData = 0x100

    @staticmethod
    def isCompressedNormalTangent(drawCall: dict[str, object]) -> bool:
        if 'm_bUseCompressedNormalTangent' in drawCall: return bool(drawCall['m_bUseCompressedNormalTangent'])
        if 'm_nFlags' not in drawCall: return False
        flags = drawCall['m_nFlags']
        match flags:
            case s if isinstance(flags, str): return 'MESH_DRAW_FLAGS_USE_COMPRESSED_NORMAL_TANGENT' in s.upper()
            case i if isinstance(flags, int): return i & RenderMeshDrawPrimitiveFlags.UseCompressedNormalTangent != 0
            case _: False

# RenderableMesh
class RenderableMesh:
    boundingBox: AABB
    tint: np.ndarray = np.ones(4)
    drawCallsAll: list[DrawCall] = []
    drawCallsOpaque: list[DrawCall] = []
    drawCallsBlended: list[DrawCall] = []
    animationTexture: int
    animationTextureSize: int
    time: float = 0.
    meshIndex: int
    mesh: IMesh
    vbib: IVBIB

    def __init__(self, action: callable, mesh: IMesh, meshIndex: int, skinMaterials: dict[str, str] = None, model: IModel = None):
        action(self)
        self.mesh = mesh
        self.vbib = model.remapBoneIndices(mesh.vbib, meshIndex) if model else mesh.vbib
        mesh.getBounds()
        self.boundingBox = AABB(mesh.minBounds, mesh.maxBounds)
        self.meshIndex = meshIndex
        self.configureDrawCalls(skinMaterials, True)
    def getSupportedRenderModes() -> list[str]: return list(set(t for s in self.drawCallsAll for t in s.shader.renderModes))
    def setRenderMode(renderMode: str) -> None: pass
    def setAnimationTexture(texture: int, animationTextureSize: int) -> None:
        self.animationTexture = texture
        self.animationTextureSize = animationTextureSize
    def update(timeStep: float) -> None: self.time += timeStep
    def setSkin(skinMaterials: dict[str, str]) -> None: self.configureDrawCalls(skinMaterials, False)
    def configureDrawCalls(skinMaterials: dict[str, str], firstSetup: bool) -> None: pass

# MeshBatchRequest
class MeshBatchRequest:
    transform: np.ndarray
    mesh: RenderableMesh
    call: DrawCall
    distanceFromCamera: float
    nodeId: int
    meshId: int

# RenderPass
class RenderPass(Enum):
    Both = 0,
    Opaque = 1,
    Translucent = 2 # Blended

# IRenderer
class IRenderer:
    boundingBox: AABB
    def render(self, camera: Camera, renderPass: RenderPass) -> None: pass
    def update(self, frameTime: float) -> None: pass

# IMeshCollection
class IMeshCollection:
    renderableMeshes: list[RenderableMesh]
