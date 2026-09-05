// PORT-SOURCE: Core/OpenStack.PolyIO/System.IO/Polyfill.cs
// PORT-SHA: 348ffb11dac35500
// PORT-STATUS: done

#[repr(C)]
#[derive(Debug, Clone, Copy, Default, PartialEq)]
pub struct X_BoundBox {
    pub min: [f32; 3], // Minimum values of X, Y, Z
    pub max: [f32; 3], // Maximum values of X, Y, Z
}
