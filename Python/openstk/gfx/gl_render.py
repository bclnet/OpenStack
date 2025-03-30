from __future__ import annotations
import math, numpy as np
from enum import Enum
from OpenGL.GL import *
from openstk.gfx.gfx_render import Camera, DrawCall, MeshBatchRequest, RenderMaterial, RenderableMesh, IPickingTexture
from openstk.gfx.gfx_scene import Scene
from openstk.gfx.gfx_ui import Key, KeyboardState, MouseState

CAMERASPEED = 300 # Per second

# typedefs
class Shader: pass
class IVBIB: pass
class IMaterial: pass
class IMesh: pass
class IModel: pass
class IOpenGLGfx: pass

# forwards
# class PickingIntent: pass
# class PixelInfo: pass
# class VAOKey: pass

# GLCamera
class GLCamera(Camera):
    mouseOverRenderArea: bool = False
    mouseState: MouseState = MouseState()
    keyboardState: KeyboardState = KeyboardState()

    class EventType(Enum):
        MouseEnter = 1
        MouseLeave = 2
        MouseMove = 3
        MouseDown = 4
        MouseUp = 5
        MouseWheel = 6
        KeyPress = 7
        KeyRelease = 8

    def __init__(self):
        super().__init__()

    def event(self, type: EventType, event: object, arg: object) -> None:
        match type:
            case self.EventType.MouseEnter: mouseOverRenderArea = True
            case self.EventType.MouseLeave: mouseOverRenderArea = False
            case self.EventType.MouseDown: (self.mouseState.leftButton, self.mouseState.rightButton) = arg
            case self.EventType.KeyPress: self.keyboardState.keys.add(arg)
            case self.EventType.KeyRelease: self.keyboardState.keys.remove(arg)

    def handleInput(self, mouseState: MouseState, keyboardState: KeyboardState) -> None: pass

    def gfxViewport(self, x: int = 0, y: int = 0, width: int = 0, height: int = 0) -> None: return glViewport(x, y, width if width != 0 else self.windowSize[0], height if height != 0 else self.windowSize[1])

# GLDebugCamera
class GLDebugCamera(GLCamera):
    mouseDragging: bool = False
    mouseDelta: np.ndarray = np.array([0., 0.])
    mousePreviousPosition: np.ndarray = np.array([0., 0.])
    keyboardState: KeyboardState = KeyboardState()
    mouseState: MouseState = MouseState()
    scrollWheelDelta: int = 0

    def __init__(self):
        super().__init__()

    def tick(self, deltaTime: int) -> None:
        if not self.mouseOverRenderArea: return

        # use the keyboard state to update position
        self._handleInputTick(deltaTime)

        # full width of the screen is a 1 PI (180deg)
        self.yaw -= math.pi * self.mouseDelta[0] / self.windowSize[0]
        self.pitch -= math.pi / self.aspectRatio * self.mouseDelta[1] / self.windowSize[1]
        self._clampRotation()
        self._recalculateMatrices()

    def handleInput(self, mouseState: MouseState, keyboardState: KeyboardState) -> None:
        self.scrollWheelDelta += mouseState.scrollWheelValue - self.mouseState.scrollWheelValue
        self.mouseState = mouseState
        self.keyboardState = keyboardState
        if self.mouseOverRenderArea or mouseState.leftButton:
            self.mouseDragging = False
            self.mouseDelta = np.array([0., 0.])
            if not self.mouseOverRenderArea: return

        # drag
        if mouseState.leftButton:
            if not self.mouseDragging: self.mouseDragging = True; self.mousePreviousPosition = np.array([mouseState.x, mouseState.y])
            mouseNewCoords = np.array([mouseState.x, mouseState.y])
            self.mouseDelta[0] = mouseNewCoords[0] - self.mousePreviousPosition[0]
            self.mouseDelta[1] = mouseNewCoords[1] - self.mousePreviousPosition[1]
            self.mousePreviousPosition = mouseNewCoords

    def _handleInputTick(self, deltaTime: float):
        speed = CAMERASPEED * deltaTime

        # double speed if shift is pressed
        if self.keyboardState.isKeyDown(Key.ShiftLeft): speed *= 2
        elif self.keyboardState.isKeyDown(Key.F): speed *= 10

        if self.keyboardState.isKeyDown(Key.W): self.location += self.getForwardVector() * speed
        if self.keyboardState.isKeyDown(Key.S): self.location -= self.getForwardVector() * speed
        if self.keyboardState.isKeyDown(Key.D): self.location += self.getRightVector() * speed
        if self.keyboardState.isKeyDown(Key.A): self.location -= self.getRightVector() * speed
        if self.keyboardState.isKeyDown(Key.Z): self.location += np.array([0., 0., -speed])
        if self.keyboardState.isKeyDown(Key.Q): self.location += np.array([0., 0., speed])

        # scroll
        if self.scrollWheelDelta: self.location += self.getForwardVector() * self.scrollWheelDelta * speed; self.scrollWheelDelta = 0

# GLMeshBuffers
class GLMeshBuffers:
    vertexBuffers: list[bytes]
    indexBuffers: list[bytes]

    class Buffer:
        handle: int
        size: int

    def __init__(self, vbib: IVBIB):
        self.vertexBuffers = [None] * vbib.vertexBuffers.Count
        self.indexBuffers = [None] * vbib.indexBuffers.Count
        for i in range(vbib.vertexBuffers.count):
            self.vertexBuffers[i].handle = glGenBuffer()
            glBindBuffer(GL_ARRAY_BUFFER, VertexBuffers[i].Handle)
            glBufferData(GL_ARRAY_BUFFER, vbib.VertexBuffers[i].ElementCount * vbib.vertexBuffers[i].elementSizeInBytes, vbib.vertexBuffers[i].data, GL_STATIC_DRAW)
            vertexBuffers[i].size = glGetBufferParameter(GL_ARRAY_BUFFER, GL_BUFFER_SIZE)
        for i in range(vbib.indexBuffers.count):
            self.indexBuffers[i].handle = glGenBuffer()
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, self.indexBuffers[i].handle)
            glBufferData(GL_ELEMENT_ARRAY_BUFFER, vbib.indexBuffers[i].elementCount * vbib.indexBuffers[i].elementSizeInBytes, vbib.indexBuffers[i].data, GL_STATIC_DRAW)
            self.indexBuffers[i].size = glGetBufferParameter(GL_ELEMENT_ARRAY_BUFFER, GL_BUFFER_SIZE)

# GLMeshBufferCache
class GLMeshBufferCache:
    _gpuBuffers: dict[IVBIB, GLMeshBuffers] = {}
    _vertexArrayObjects: dict[VAOKey, int] = {}

    class VAOKey:
        vbib: GLMeshBuffers
        shader: Shader
        vertexIndex: int
        indexIndex: int
        baseVertex: int
    
    def getVertexIndexBuffers(vbib: IVBIB) -> GLMeshBuffers:
        # cache
        if vbib in self._gpuBuffers: return self._gpuBuffers[vbib]
        # build
        newGpuVbib = GLMeshBuffers(vbib)
        self._gpuBuffers.append(vbib, newGpuVbib)
        return newGpuVbib

    def getVertexArrayObject(vbib: IVBIB, shader: Shader, vtxIndex: int, idxIndex: int, baseVertex: int) -> int:
        gpuVbib = getVertexIndexBuffers(vbib)
        vaoKey = VAOKey(
            vbib = gpuVbib,
            shader = shader,
            vertexIndex = vtxIndex,
            indexIndex = idxIndex,
            baseVertex = baseVertex)
        # cache
        if vaoKey in self._vertexArrayObjects: return self._vertexArrayObjects[vaoKey]
        # build
        newVaoHandle = glGenVertexArrays(1)
        glBindVertexArray(newVaoHandle)
        glBindBuffer(GL_ARRAY_BUFFER, gpuVbib.vertexBuffers[vtxIndex].handle)
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, gpuVbib.indexBuffers[idxIndex].handle)
        curVertexBuffer = vbib.vertexBuffers[vtxIndex]
        texCoordNum = 0
        colorNum = 0
        for attribute in curVertexBuffer.attributes:
            attributeName = f'v{attribute.SemanticName}'
            if attribute.SemanticName == 'TEXCOORD' and (texCoordNum := texCoordNum + 1) > 0: attributeName += texCoordNum
            elif attribute.SemanticName == 'COLOR' and (colorNum := colorNum + 1) > 0: attributeName += colorNum
            bindVertexAttrib(attribute, attributeName, shader.Program, curVertexBuffer.elementSizeInBytes, baseVertex)
        glBindVertexArray(0)
        _vertexArrayObjects.append(vaoKey, newVaoHandle)
        return newVaoHandle

    @staticmethod
    def bindVertexAttrib(attribute: object, attributeName: str, shaderProgram: int, stride: int, baseVertex: int) -> None:
        attributeLocation = glGetAttribLocation(shaderProgram, attributeName)
        if attributeLocation == -1: return; # ignore this attribute if it is not found in the shader
        glEnableVertexAttribArray(attributeLocation)
        match attribute.Format:
            case DXGI_FORMAT.R32G32B32_FLOAT: glVertexAttribPointer(attributeLocation, 3, GL_FLOAT, false, stride, baseVertex + attribute.offset)
            case DXGI_FORMAT.R8G8B8A8_UNORM: glVertexAttribPointer(attributeLocation, 4, GL_UNSIGNED_BYTE, false, stride, baseVertex + attribute.offset)
            case DXGI_FORMAT.R32G32_FLOAT: glVertexAttribPointer(attributeLocation, 2, GL_FLOAT, false, stride, baseVertex + attribute.offset)
            case DXGI_FORMAT.R16G16_FLOAT: glVertexAttribPointer(attributeLocation, 2, GL_HALF_FLOAT, false, stride, baseVertex + attribute.offset)
            case DXGI_FORMAT.R32G32B32A32_FLOAT: glVertexAttribPointer(attributeLocation, 4, GL_FLOAT, false, stride, baseVertex + attribute.offset)
            case DXGI_FORMAT.R8G8B8A8_UINT: glVertexAttribPointer(attributeLocation, 4, GL_UNSIGNED_BYTE, false, stride, baseVertex + attribute.offset)
            case DXGI_FORMAT.R16G16_SINT: glVertexAttribIPointer(attributeLocation, 2, GL_SHORT, stride, baseVertex + attribute.offset)
            case DXGI_FORMAT.R16G16B16A16_SINT: glVertexAttribIPointer(attributeLocation, 4, GL_SHORT, stride, baseVertex + attribute.offset)
            case DXGI_FORMAT.R16G16_SNORM: glVertexAttribPointer(attributeLocation, 2, GL_SHORT, true, stride, baseVertex + attribute.offset)
            case DXGI_FORMAT.R16G16_UNORM: glVertexAttribPointer(attributeLocation, 2, GL_UNSIGNED_SHORT, true, stride, baseVertex + attribute.offset)
            case _: raise Exception(f'Unknown attribute format {attribute.Format}')

# MeshBatchRenderer
class MeshBatchRenderer:
    @staticmethod
    def render(requests: list[MeshBatchRequest], context: Scene.RenderContext) -> None:
            # opaque: grouped by material
            if context.renderPass == RenderPass.Both or context.renderPass == RenderPass.Opaque: self.drawBatch(requests, context)
            # blended: in reverse order
            if context.renderPass == RenderPass.Both or context.enderPass == renderPass.Translucent:
                holder = [MeshBatchRequest()] # holds the one request we render at a time
                requests.sort(lambda a, b: -a.distanceFromCamera.compareTo(b.distanceFromCamera))
                for request in requests: holder[0] = request; self.drawBatch(holder, context)

    def drawBatch(drawCalls: list[MeshBatchRequest], context: Scene.RenderContext) -> None:
        glEnable(GL_DEPTH_TEST)

        viewProjectionMatrix = context.camera.viewProjectionMatrix
        cameraPosition = context.camera.location
        lightPosition = cameraPosition # (context.LightPosition ?? context.Camera.Location)

        # groups
        groupedDrawCalls = drawCalls.groupBy(lambda a: a.call.shader) if not context.replacementShader else \
            drawCalls.groupBy(lambda a: context.replacementShader)
        for shaderGroup in groupedDrawCalls:
            shader = shaderGroup.key
            uniformLocationAnimated = shader.getUniformLocation('bAnimated')
            uniformLocationAnimationTexture = shader.getUniformLocation('animationTexture')
            uniformLocationNumBones = shader.getUniformLocation('fNumBones')
            uniformLocationTransform = shader.getUniformLocation('transform')
            uniformLocationTint = shader.getUniformLocation('m_vTintColorSceneObject')
            uniformLocationTintDrawCall = shader.getUniformLocation('m_vTintColorDrawCall')
            uniformLocationTime = shader.getUniformLocation('g_flTime')
            uniformLocationObjectId = shader.getUniformLocation('sceneObjectId')
            uniformLocationMeshId = shader.getUniformLocation('meshId')
            glUseProgram(shader.program)
            glUniform3(shader.getUniformLocation('vLightPosition'), cameraPosition)
            glUniform3(shader.getUniformLocation('vEyePosition'), cameraPosition)
            glUniformMatrix4(shader.getUniformLocation('uProjectionViewMatrix'), False) # ref viewProjectionMatrix

            # materials
            for materialGroup in shaderGroup.groupBy(lambda a: a.call.material):
                material = materialGroup.key
                if not context.showDebug and material.isToolsMaterial: continue
                material.render(shader)
                for request in materialGroup:
                    transform = request.transform
                    transform = glUniformMatrix4(uniformLocationTransform, False)
                    if uniformLocationObjectId != 1: glUniform1(uniformLocationObjectId, request.nodeId)
                    if uniformLocationMeshId != 1: glUniform1(uniformLocationMeshId, request.meshId)
                    if uniformLocationTime != 1: glUniform1(uniformLocationTime, request.mesh.time)
                    if uniformLocationAnimated != -1: glUniform1(uniformLocationAnimated, 1. if request.mesh.animationTexture != None else 0.)

                    # push animation texture to the shader (if it supports it)
                    if request.mesh.animationTexture != None:
                        if uniformLocationAnimationTexture != -1: glActiveTexture(GL_TEXTURE0); glBindTexture(GL_TEXTURE_2D, request.mesh.animationTexture.value); glUniform1(uniformLocationAnimationTexture, 0)
                        if uniformLocationNumBones != -1: v = math.max(1, request.mesh.animationTextureSize - 1); glUniform1(uniformLocationNumBones, v)

                    # draw
                    if uniformLocationTint > -1: tint = request.mesh.tint; glUniform4(uniformLocationTint, tint)
                    if uniformLocationTintDrawCall > -1: glUniform3(uniformLocationTintDrawCall, request.call.tintColor)
                    glBindVertexArray(request.call.vertexArrayObject)
                    glDrawElements(request.call.primitiveType, request.call.indexCount, request.call.IndexType, request.call.startIndex)
                material.postRender()
        glDisable(GL_DEPTH_TEST)

# QuadIndexBuffer
class QuadIndexBuffer:
    glHandle: int

    def __init__(self, size: int):
        self.glHandle = glGenBuffer()
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, self.glHandle)
        indices = []*size
        for i in range(size / 6):
            indices[(i * 6) + 0] = ((i * 4) + 0)
            indices[(i * 6) + 1] = ((i * 4) + 1)
            indices[(i * 6) + 2] = ((i * 4) + 2)
            indices[(i * 6) + 3] = ((i * 4) + 0)
            indices[(i * 6) + 4] = ((i * 4) + 2)
            indices[(i * 6) + 5] = ((i * 4) + 3)
        glBufferData(GL_ELEMENT_ARRAY_BUFFER, size * 2, indices, GL_STATIC_DRAW)

# GLPickingTexture
class GLPickingTexture(IPickingTexture):
    class PixelInfo:
        objectId: int
        meshId: int
        unused2: int

    class PickingIntent(Enum):
        Select = 1,
        Open = 2

    class PickingRequest:
        activeNextFrame: bool
        cursorPositionX: int
        cursorPositionY: int
        intent: PickingIntent
        def nextFrame(self, x: int, y: int, intent: PickingIntent):
            self.activeNextFrame = True
            self.cursorPositionX = x
            self.cursorPositionY = y
            self.intent = intent

    class PickingResponse:
        intent: PickingIntent
        pixelInfo: PixelInfo
        
    onPicked: object
    request: PickingRequest = PickingRequest()
    shader: Shader
    debugShader: Shader
    @property
    def isActive(self) -> bool: self.request.activeNextFrame
    debug: bool
    width: int = 4
    height: int = 4
    fboHandle: int
    colorHandle: int
    depthHandle: int

    def __init__(self, gfx: IOpenGLGfx3d, onPicked: list[callable]):
        self.shader, _ = gfx.createShader('vrf.picking', {})
        self.debugShader, _ = gfx.createShader('vrf.picking', { 'F_DEBUG_PICKER': True })
        # self.onPicked += onPicked
        self.setup()

    def dispose(self):
        self.onPicked = None
        glDeleteTexture(self.colorHandle)
        glDeleteTexture(self.depthHandle)
        glDeleteFramebuffer(self.fboHandle)

    def setup(self) -> None:
        self.fboHandle = glGenFramebuffers(1)
        glBindFramebuffer(GL_FRAMEBUFFER, self.fboHandle)

        # color
        self.colorHandle = glGenTextures(1)
        glBindTexture(GL_TEXTURE_2D, self.colorHandle)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32UI, self.width, self.height, 0, GL_RGBA_INTEGER, GL_UNSIGNED_INT, None)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST)
        glTexParameter(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST)
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, self.colorHandle, 0)

        # depth
        self.depthHandle = glGenTextures(1)
        glBindTexture(GL_TEXTURE_2D, self.depthHandle)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, self.width, self.height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, None)
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, self.depthHandle, 0)

        # bind
        status = glCheckFramebufferStatus(GL_FRAMEBUFFER)
        if status != GL_FRAMEBUFFER_COMPLETE: raise Exception(f'Framebuffer failed to bind with error: {status}')
        glBindTexture(GL_TEXTURE_2D, 0)
        glBindFramebuffer(GL_FRAMEBUFFER, 0)

    def render(self) -> None:
        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, self.fboHandle)
        glClearColor(0., 0., 0., 0.)
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)

    def finish(self) -> None:
        glBindFramebuffer(dl.GL_DRAW_FRAMEBUFFER, 0)
        if self.request.activeNextFrame:
            self.request.activeNextFrame = False
            pixelInfo = ReadPixelInfo(self.request.cursorPositionX, self.request.cursorPositionY)
            if self.onPicked:
                self.onPicked.invoke(self, PickingResponse(
                    intent = Request.Intent,
                    pixelInfo = pixelInfo))

    def resize(self, width: int, height: int) -> None:
        self.width = width
        self.height = height
        glBindTexture(GL_TEXTURE_2D, self.colorHandle)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA32UI, self.width, self.height, 0, GL_RGBA_INTEGER, GL_UNSIGNED_INT, IntPtr.Zero)
        glBindTexture(GL_TEXTURE_2D, self.depthHandle)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, self.width, self.height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, IntPtr.Zero)

    def readPixelInfo(self, width: int, height: int) -> PixelInfo:
        glFlush()
        glFinish()
        glBindFramebuffer(GL_READ_FRAMEBUFFER, self.fboHandle)
        glReadBuffer(GL_COLOR_ATTACHMENT0)
        # pixelInfo = PixelInfo()
        pixelInfo = glReadPixels(width, self.height - height, 1, 1, GL_RGBA_INTEGER, GL_UNSIGNED_INT)
        glReadBuffer(GL_NONE)
        glBindFramebuffer(GL_READ_FRAMEBUFFER, 0)
        return pixelInfo

# GLRenderMaterial
class GLRenderMaterial(RenderMaterial):
    def __init__(self, material: IMaterial):
        super().__init__(material)

    def render(self, shader: Shader) -> None:
        # start at 1, texture unit 0 is reserved for the animation texture
        textureUnit = 1
        location: int
        for texture in self.textures:
            location = shader.getUniformLocation(texture.key)
            if location > -1:
                glActiveTexture(GL_TEXTURE0 + textureUnit)
                glBindTexture(GL_TEXTURE_2D, texture.Value)
                glUniform1(location, textureUnit)
                textureUnit += 1
        match self.material:
            case p if isinstance(self.material, MaterialMapProp):
                for param in p.intParams:
                    location = shader.getUniformLocation(param.key)
                    if location > -1: glUniform1(location, param.value)
                for param in p.floatParams:
                    location = shader.getUniformLocation(param.key)
                    if location > -1: glUniform1(location, param.value)
                for param in p.vectorParams:
                    location = shader.getUniformLocation(param.key)
                    if location > -1: glUniform4(location, np.array([param.value[0], param.value[1], param.value[2], param.value[3]]))
        alphaReference: int = shader.getUniformLocation('g_flAlphaTestReference')
        if alphaReference > -1: glUniform1(alphaReference, alphaTestReference)
        if self.isBlended:
            glDepthMask(False)
            glEnable(GL_BLEND)
            glBlendFunc(GL_SRC_ALPHA, GL_ONE if self.isAdditiveBlend else GL_ONE_MINUS_SRC_ALPHA)
        if self.isRenderBackfaces: glDisable(GL_CULL_FACE)

    def postRender(self) -> None:
        if self.isBlended: glDepthMask(True); glDisable(GL_BLEND)
        if self.isRenderBackfaces: glEnable(GL_CULL_FACE)

# GLRenderableMesh
class GLRenderableMesh(RenderableMesh):
    graphic: IOpenGLGfx

    def __init__(self, graphic: IOpenGLGfx, mesh: IMesh, meshIndex: int, skinMaterials: dict[str, str] = None, model: IModel = None):
        super().__init__(lambda t: print('TODO: t.graphic = graphic'), mesh, meshIndex, skinMaterials, model)

    def setRenderMode(self, renderMode: str):
        for call in (self.drawCallsOpaque + self.drawCallsBlended):
            # recycle old shader parameters that are not render modes since we are scrapping those anyway
            parameters = { x.key:x.value for x in call.shader.parameters if x.key.startsWith('renderMode') }
            if renderMode and call.shader.renderModes.contains(renderMode): parameters.append(f'renderMode_{renderMode}', True)
            (call.shader, _) = self.graphic.createShader(call.shader.name, parameters)
            call.vertexArrayObject = self.graphic.meshBufferCache.getVertexArrayObject(self.mesh.vbib, call.shader, call.vertexBuffer.id, call.indexBuffer.id, call.baseVertex)

    def _configureDrawCalls(skinMaterials: dict[str, str], firstSetup: bool) -> None:
        data = mesh.data
        if firstSetup: self.graphic.meshBufferCache.getVertexIndexBuffers(self.vbib) # this call has side effects because it uploads to gpu

        # prepare drawcalls
        i = 0
        for sceneObject in data['m_sceneObjects']:
            for objectDrawCall in sceneObject['m_drawCalls']:
                materialName = objectDrawCall['m_material'] or objectDrawCall['m_pMaterial']
                if skinMaterials and materialName in skinMaterials: materialName = skinMaterials[materialName]
                material, _ = self.graphic.materialManager.loadMaterial(f'{materialName}_c')
                isOverlay = isinstance(material.material, IParamMaterial) and 'F_OVERLAY' in material.material.intParams
                if isOverlay: continue # ignore overlays for now
                shaderArgs: dict[str, bool] = {}
                if drawCall.isCompressedNormalTangent(objectDrawCall): shaderArgs['fulltangent'] = False
                # first
                if firstSetup:
                    drawCall = self.createDrawCall(objectDrawCall, shaderArgs, material)
                    self.drawCallsAll.append(drawCall)
                    if drawCall.material.isBlended: self.drawCallsBlended.append(drawCall)
                    else: self.drawCallsOpaque.append(drawCall)
                    continue
                # next
                self.setupDrawCallMaterial(self.drawCallsAll[i], shaderArgs, material); i += 1

    def _createDrawCall(objectDrawCall: dict[str, object], shaderArgs: dict[str, bool], material: GLRenderMaterial) -> DrawCall:
        drawCall = DrawCall()
        primitiveType = objectDrawCall['m_nPrimitiveType']
        match primitiveType:
            case i if isinstance(primitiveType, int):
                if i == RenderPrimitiveType.RENDER_PRIM_TRIANGLES: drawCall.primitiveType = GL_TRIANGLES
            case s if isinstance(primitiveType, str):
                if s == 'RENDER_PRIM_TRIANGLES': drawCall.primitiveType = GL_TRIANGLES
        if drawCall.primitiveType != GL_TRIANGLES: raise Exception(f'Unknown PrimitiveType in drawCall! {primitiveType})')
        # material
        self.setupDrawCallMaterial(drawCall, shaderArgs, material)
        # index-buffer
        indexBufferObject = objectDrawCall['m_indexBuffer']
        drawCall.indexBuffer = (indexBufferObject['m_hBuffer'], indexBufferObject['m_nBindOffsetBytes'])
        # vertex
        vertexElementSize = self.vbib.vertexBuffers[drawCall.vertexBuffer.id].elementSizeInBytes
        drawCall.baseVertex = objectDrawCall['m_nBaseVertex'] * vertexElementSize
        # index
        indexElementSize = self.vbib.indexBuffers[drawCall.indexBuffer.id].elementSizeInBytes
        drawCall.startIndex = objectDrawCall['m_nStartIndex'] * indexElementSize
        drawCall.indexCount = objectDrawCall['m_nIndexCount']
        # tint
        if 'm_vTintColor' in objectDrawCall: drawCall.tintColor = objectDrawCall['m_vTintColor']
        # index-type
        if indexElementSize == 2: drawCall.indexType = GL_UNSIGNED_SHORT; # shopkeeper_vr
        elif indexElementSize == 4: drawCall.indexType = GL_UNSIGNED_INT; # glados
        else: raise Exception(f'Unsupported index type {indexElementSize}')
        # vbo
        vertexBuffer = objectDrawCall['m_vertexBuffers'][0]
        drawCall.vertexBuffer = (vertexBuffer['m_hBuffer'], vertexBuffer['m_nBindOffsetBytes'])
        drawCall.vertexArrayObject = self.graphic.meshBufferCache.getVertexArrayObject(self.vbib, drawCall.shader, drawCall.vertexBuffer.id, drawCall.indexBuffer.id, drawCall.baseVertex)
        return drawCall

    def _setupDrawCallMaterial(drawCall: DrawCall, shaderArgs: dict[str, bool], material: RenderMaterial) -> None:
        drawCall.material = material
        # add shader parameters from material to the shader parameters from the draw call
        combinedShaderArgs = { x.key:x.value for x in shaderArgs + material.material.getShaderArgs() }
        # load shader
        drawCall.shader, _ = self.graphic.loadShader(drawCall.material.material.shaderName, combinedShaderArgs)
        # bind and validate shader
        glUseProgram(drawCall.shader.program)
        # tint and normal
        if 'g_tTintMask' not in drawCall.material.textures: drawCall.material.textures.append('g_tTintMask', self.graphic.textureManager.buildSolidTexture(1, 1, 1., 1., 1., 1.))
        if 'g_tNormal' not in drawCall.material.textures: drawCall.material.textures.append('g_tNormal', self.graphic.textureManager.buildSolidTexture(1, 1, 0.5, 1, 0.5, 1))