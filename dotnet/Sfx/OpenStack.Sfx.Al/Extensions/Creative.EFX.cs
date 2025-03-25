using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenStack.Sfx.Al.Extensions.Creative.EFX;

/// <summary>
/// Exposes the functions of the Effects Extension.
/// </summary>
public class EFX : ALBase
{
    /// <summary>
    /// The EFX extension name.
    /// </summary>
    public const string ExtensionName = "ALC_EXT_EFX";

    // We need to register the resolver for OpenAL before we can DllImport functions.
    static EFX() => RegisterOpenALResolver();
    EFX() { }

    /// <summary>
    /// Checks if this extension is present.
    /// </summary>
    /// <param name="device">The device to query.</param>
    /// <returns>Whether the extension was present or not.</returns>
    public static bool IsExtensionPresent(ALDevice device) => ALC.IsExtensionPresent(device, ExtensionName);

    /// <summary>
    /// Gets a vector of integer properties from the context.
    /// </summary>
    /// <param name="device">The audio device.</param>
    /// <param name="param">The named property.</param>
    /// <param name="size">The size of the provided buffer.</param>
    /// <param name="data">A pointer to the first element of a provided data buffer.</param>
    [DllImport(ALC.Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = ALC.AlcCallingConv)] public static extern unsafe void GetInteger(ALDevice device, EFXContextInteger param, int size, int* data);

    /// <summary>
    /// Gets a vector of integer properties from the context.
    /// </summary>
    /// <param name="device">The audio device.</param>
    /// <param name="param">The named property.</param>
    /// <param name="size">The size of the provided buffer.</param>
    /// <param name="data">A pointer to the first element of a provided data buffer.</param>
    [DllImport(ALC.Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = ALC.AlcCallingConv)] public static extern void GetInteger(ALDevice device, EFXContextInteger param, int size, ref int data);

    /// <summary>
    /// Gets a vector of integer properties from the context.
    /// </summary>
    /// <param name="device">The audio device.</param>
    /// <param name="param">The named property.</param>
    /// <param name="size">The size of the provided buffer.</param>
    /// <param name="data">A pointer to the first element of a provided data buffer.</param>
    [DllImport(ALC.Lib, EntryPoint = "alcGetIntegerv", ExactSpelling = true, CallingConvention = ALC.AlcCallingConv)] public static extern void GetInteger(ALDevice device, EFXContextInteger param, int size, int[] data);

    /// <summary>
    /// Gets a vector of integer properties from the context.
    /// </summary>
    /// <param name="device">The audio device.</param>
    /// <param name="param">The named property.</param>
    /// <param name="data">A provided data buffer.</param>
    public static void GetInteger(ALDevice device, EFXContextInteger param, int[] data) => GetInteger(device, param, data.Length, data);

    /// <summary>
    /// Gets the major version of the Effect Extension.
    /// </summary>
    /// <param name="device">The device that the context is on.</param>
    /// <returns>The major version.</returns>
    public int GetEFXMajorVersion(ALDevice device) { var result = 0; GetInteger(device, EFXContextInteger.EFXMajorVersion, 1, ref result); return result; }

    /// <summary>
    /// Gets the minor version of the Effect Extension.
    /// </summary>
    /// <param name="device">The device that the context is on.</param>
    /// <returns>The minor version.</returns>
    public int GetEFXMinorVersion(ALDevice device) { var result = 0; GetInteger(device, EFXContextInteger.EFXMinorVersion, 1, ref result); return result; }

    /// <summary>
    /// Gets the version of the Effect Extension.
    /// </summary>
    /// <param name="device">The device that the context is on.</param>
    /// <returns>The version.</returns>
    public Version GetEFXVersion(ALDevice device) => new Version(GetEFXMajorVersion(device), GetEFXMinorVersion(device));

    #region Generated

    /// <summary>
    /// Creates one or more auxiliary effect slots.
    /// </summary>
    /// <param name="count">The number of slots to create.</param>
    /// <param name="slots">The first element of the array to place the slots into.</param>
    /// <seealso cref="DeleteAuxiliaryEffectSlots(int, int*)"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static unsafe void GenAuxiliaryEffectSlots(int count, int* slots) => _GenAuxiliaryEffectSlotsPtr(count, slots);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GenAuxiliaryEffectSlotsPtrDelegate(int count, int* slots);
    static readonly GenAuxiliaryEffectSlotsPtrDelegate _GenAuxiliaryEffectSlotsPtr = LoadDelegate<GenAuxiliaryEffectSlotsPtrDelegate>("alGenAuxiliaryEffectSlots");

    /// <summary>
    /// Creates one or more auxiliary effect slots.
    /// </summary>
    /// <param name="count">The number of slots to create.</param>
    /// <param name="slots">The first element of the array to place the slots into.</param>
    /// <seealso cref="DeleteAuxiliaryEffectSlots(int, ref int)"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static void GenAuxiliaryEffectSlots(int count, ref int slots) => _GenAuxiliaryEffectSlotsRef(count, ref slots);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GenAuxiliaryEffectSlotsRefDelegate(int count, ref int slots);
    static readonly GenAuxiliaryEffectSlotsRefDelegate _GenAuxiliaryEffectSlotsRef = LoadDelegate<GenAuxiliaryEffectSlotsRefDelegate>("alGenAuxiliaryEffectSlots");

    /// <summary>
    /// Creates one or more auxiliary effect slots.
    /// </summary>
    /// <param name="count">The number of slots to create.</param>
    /// <param name="slots">The first element of the array to place the slots into.</param>
    /// <seealso cref="DeleteAuxiliaryEffectSlots(int, int[])"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static void GenAuxiliaryEffectSlots(int count, int[] slots) => _GenAuxiliaryEffectSlotsArray(count, slots);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GenAuxiliaryEffectSlotsArrayDelegate(int count, int[] slots);
    static readonly GenAuxiliaryEffectSlotsArrayDelegate _GenAuxiliaryEffectSlotsArray = LoadDelegate<GenAuxiliaryEffectSlotsArrayDelegate>("alGenAuxiliaryEffectSlots");

    /// <summary>
    /// Deletes and frees resources used for a set of auxiliary effect slots.
    /// </summary>
    /// <param name="count">The number of slots to delete.</param>
    /// <param name="slots">A pointer to the array of slots to delete.</param>
    /// <seealso cref="GenAuxiliaryEffectSlots(int, int*)"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static unsafe void DeleteAuxiliaryEffectSlots(int count, int* slots) => _DeleteAuxiliaryEffectSlotsPtr(count, slots);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void DeleteAuxiliaryEffectSlotsPtrDelegate(int count, int* slots);
    static readonly DeleteAuxiliaryEffectSlotsPtrDelegate _DeleteAuxiliaryEffectSlotsPtr = LoadDelegate<DeleteAuxiliaryEffectSlotsPtrDelegate>("alDeleteAuxiliaryEffectSlots");

    /// <summary>
    /// Deletes and frees resources used for a set of auxiliary effect slots.
    /// </summary>
    /// <param name="count">The number of slots to delete.</param>
    /// <param name="slots">A pointer to the array of slots to delete.</param>
    /// <seealso cref="GenAuxiliaryEffectSlots(int, ref int)"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static void DeleteAuxiliaryEffectSlots(int count, ref int slots) => _DeleteAuxiliaryEffectSlotsRef(count, ref slots);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void DeleteAuxiliaryEffectSlotsRefDelegate(int count, ref int slots);
    static readonly DeleteAuxiliaryEffectSlotsRefDelegate _DeleteAuxiliaryEffectSlotsRef = LoadDelegate<DeleteAuxiliaryEffectSlotsRefDelegate>("alDeleteAuxiliaryEffectSlots");

    /// <summary>
    /// Deletes and frees resources used for a set of auxiliary effect slots.
    /// </summary>
    /// <param name="count">The number of slots to delete.</param>
    /// <param name="slots">A pointer to the array of slots to delete.</param>
    /// <seealso cref="GenAuxiliaryEffectSlots(int, int[])"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static void DeleteAuxiliaryEffectSlots(int count, int[] slots) => _DeleteAuxiliaryEffectSlotsArray(count, slots);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void DeleteAuxiliaryEffectSlotsArrayDelegate(int count, int[] slots);
    static readonly DeleteAuxiliaryEffectSlotsArrayDelegate _DeleteAuxiliaryEffectSlotsArray = LoadDelegate<DeleteAuxiliaryEffectSlotsArrayDelegate>("alDeleteAuxiliaryEffectSlots");

    /// <summary>
    /// Determines whether or not the given handle is an auxiliary slot handle.
    /// </summary>
    /// <param name="slot">The handle.</param>
    /// <returns>true if the handle is a slot handle; otherwise, false.</returns>
    public static bool IsAuxiliaryEffectSlot(int slot) => _IsAuxiliaryEffectSlot(slot);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate bool IsAuxiliaryEffectSlotDelegate(int slot);
    static readonly IsAuxiliaryEffectSlotDelegate _IsAuxiliaryEffectSlot = LoadDelegate<IsAuxiliaryEffectSlotDelegate>("alIsAuxiliaryEffectSlot");

    /// <summary>
    /// Sets the value of a named property on the given effect slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void AuxiliaryEffectSlot(int slot, EffectSlotInteger param, int value) => _AuxiliaryEffectSloti(slot, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void AuxiliaryEffectSlotiDelegate(int slot, EffectSlotInteger param, int value);
    static readonly AuxiliaryEffectSlotiDelegate _AuxiliaryEffectSloti = LoadDelegate<AuxiliaryEffectSlotiDelegate>("alAuxiliaryEffectSloti");

    /// <summary>
    /// Sets the value of a named property on the given effect slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void AuxiliaryEffectSlot(int slot, EffectSlotFloat param, float value) => _AuxiliaryEffectSlotf(slot, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void AuxiliaryEffectSlotfDelegate(int slot, EffectSlotFloat param, float value);
    static readonly AuxiliaryEffectSlotfDelegate _AuxiliaryEffectSlotf = LoadDelegate<AuxiliaryEffectSlotfDelegate>("alAuxiliaryEffectSlotf");

    /// <summary>
    /// Gets the value of a named property on the given effect slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value.</param>
    public static unsafe void GetAuxiliaryEffectSlot(int slot, EffectSlotInteger param, int* value) => _GetAuxiliaryEffectSlotiPtr(slot, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetAuxiliaryEffectSlotiPtrDelegate(int slot, EffectSlotInteger param, int* value);
    static readonly GetAuxiliaryEffectSlotiPtrDelegate _GetAuxiliaryEffectSlotiPtr = LoadDelegate<GetAuxiliaryEffectSlotiPtrDelegate>("alGetAuxiliaryEffectSloti");

    /// <summary>
    /// Gets the value of a named property on the given effect slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value.</param>
    public static void GetAuxiliaryEffectSlot(int slot, EffectSlotInteger param, out int value) => _GetAuxiliaryEffectSlotiRef(slot, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetAuxiliaryEffectSlotiRefDelegate(int slot, EffectSlotInteger param, out int value);
    static readonly GetAuxiliaryEffectSlotiRefDelegate _GetAuxiliaryEffectSlotiRef = LoadDelegate<GetAuxiliaryEffectSlotiRefDelegate>("alGetAuxiliaryEffectSloti");

    /// <summary>
    /// Gets the value of a named property on the given effect slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value.</param>
    public static unsafe void GetAuxiliaryEffectSlot(int slot, EffectSlotFloat param, float* value) => _GetAuxiliaryEffectSlotfPtr(slot, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetAuxiliaryEffectSlotfPtrDelegate(int slot, EffectSlotFloat param, float* value);
    static readonly GetAuxiliaryEffectSlotfPtrDelegate _GetAuxiliaryEffectSlotfPtr = LoadDelegate<GetAuxiliaryEffectSlotfPtrDelegate>("alGetAuxiliaryEffectSlotf");

    /// <summary>
    /// Gets the value of a named property on the given effect slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value.</param>
    public static void GetAuxiliaryEffectSlot(int slot, EffectSlotFloat param, out float value) => _GetAuxiliaryEffectSlotfRef(slot, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetAuxiliaryEffectSlotfRefDelegate(int slot, EffectSlotFloat param, out float value);
    static readonly GetAuxiliaryEffectSlotfRefDelegate _GetAuxiliaryEffectSlotfRef = LoadDelegate<GetAuxiliaryEffectSlotfRefDelegate>("alGetAuxiliaryEffectSlotf");

    /// <summary>
    /// Creates one or more effect objects.
    /// </summary>
    /// <param name="count">The number of objects to generate.</param>
    /// <param name="effects">A pointer to the first element of the array where the handles will be stored.</param>
    /// <seealso cref="DeleteEffects(int, int*)"/>
    /// <seealso cref="IsEffect(int)"/>
    public static unsafe void GenEffects(int count, int* effects) => _GenEffectsPtr(count, effects);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GenEffectsPtrDelegate(int count, int* effects);
    static readonly GenEffectsPtrDelegate _GenEffectsPtr = LoadDelegate<GenEffectsPtrDelegate>("alGenEffects");

    /// <summary>
    /// Creates one or more effect objects.
    /// </summary>
    /// <param name="count">The number of objects to generate.</param>
    /// <param name="effects">A pointer to the first element of the array where the handles will be stored.</param>
    /// <seealso cref="DeleteEffects(int, ref int)"/>
    /// <seealso cref="IsEffect(int)"/>
    public static void GenEffects(int count, ref int effects) => _GenEffectsRef(count, ref effects);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GenEffectsRefDelegate(int count, ref int effects);
    static readonly GenEffectsRefDelegate _GenEffectsRef = LoadDelegate<GenEffectsRefDelegate>("alGenEffects");

    /// <summary>
    /// Creates one or more effect objects.
    /// </summary>
    /// <param name="count">The number of objects to generate.</param>
    /// <param name="effects">A pointer to the first element of the array where the handles will be stored.</param>
    /// <seealso cref="DeleteEffects(int, int[])"/>
    /// <seealso cref="IsEffect(int)"/>
    public static void GenEffects(int count, int[] effects) => _GenEffectsArray(count, effects);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GenEffectsArrayDelegate(int count, int[] effects);
    static readonly GenEffectsArrayDelegate _GenEffectsArray = LoadDelegate<GenEffectsArrayDelegate>("alGenEffects");

    /// <summary>
    /// Deletes one or more effect objects, freeing their resources.
    /// </summary>
    /// <param name="count">The number of objects to delete.</param>
    /// <param name="effects">A pointer to the first element of the array where the handles are stored.</param>
    /// <seealso cref="GenEffects(int, int*)"/>
    /// <seealso cref="IsEffect(int)"/>
    public static unsafe void DeleteEffects(int count, int* effects) => _DeleteEffectsPtr(count, effects);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void DeleteEffectsPtrDelegate(int count, int* effects);
    static readonly DeleteEffectsPtrDelegate _DeleteEffectsPtr = LoadDelegate<DeleteEffectsPtrDelegate>("alDeleteEffects");

    /// <summary>
    /// Deletes one or more effect objects, freeing their resources.
    /// </summary>
    /// <param name="count">The number of objects to delete.</param>
    /// <param name="effects">A pointer to the first element of the array where the handles are stored.</param>
    /// <seealso cref="GenEffects(int, ref int)"/>
    /// <seealso cref="IsEffect(int)"/>
    public static void DeleteEffects(int count, ref int effects) => _DeleteEffectsRef(count, ref effects);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void DeleteEffectsRefDelegate(int count, ref int effects);
    static readonly DeleteEffectsRefDelegate _DeleteEffectsRef = LoadDelegate<DeleteEffectsRefDelegate>("alDeleteEffects");

    /// <summary>
    /// Deletes one or more effect objects, freeing their resources.
    /// </summary>
    /// <param name="count">The number of objects to delete.</param>
    /// <param name="effects">A pointer to the first element of the array where the handles are stored.</param>
    /// <seealso cref="GenEffects(int, int[])"/>
    /// <seealso cref="IsEffect(int)"/>
    public static void DeleteEffects(int count, int[] effects) => _DeleteEffectsArray(count, effects);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void DeleteEffectsArrayDelegate(int count, int[] effects);
    static readonly DeleteEffectsArrayDelegate _DeleteEffectsArray = LoadDelegate<DeleteEffectsArrayDelegate>("alDeleteEffects");

    /// <summary>
    /// Determines whether or not a given handle is an effect handle.
    /// </summary>
    /// <param name="effect">The handle.</param>
    /// <returns>true if the handle is an effect handle; otherwise, false.</returns>
    /// <seealso cref="GenEffects(int[])"/>
    /// <seealso cref="DeleteEffects(int[])"/>
    public static bool IsEffect(int effect) => _IsEffect(effect);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate bool IsEffectDelegate(int effect);
    static readonly IsEffectDelegate _IsEffect = LoadDelegate<IsEffectDelegate>("alIsEffect");

    /// <summary>
    /// Sets the value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Effect(int effect, EffectInteger param, int value) => _Effecti(effect, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void EffectiDelegate(int effect, EffectInteger param, int value);
    static readonly EffectiDelegate _Effecti = LoadDelegate<EffectiDelegate>("alEffecti");

    /// <summary>
    /// Sets the value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Effect(int effect, EffectFloat param, float value) => _Effectf(effect, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void EffectfDelegate(int effect, EffectFloat param, float value);
    static readonly EffectfDelegate _Effectf = LoadDelegate<EffectfDelegate>("alEffectf");

    /// <summary>
    /// Sets the vector value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void Effect(int effect, EffectVector3 param, float* value) => _EffectfvPtr(effect, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void EffectfvPtrDelegate(int effect, EffectVector3 param, float* value);
    static readonly EffectfvPtrDelegate _EffectfvPtr = LoadDelegate<EffectfvPtrDelegate>("alEffectfv");

    /// <summary>
    /// Sets the vector value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Effect(int effect, EffectVector3 param, ref float value) => _EffectfvRef(effect, param, ref value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void EffectfvRefDelegate(int effect, EffectVector3 param, ref float value);
    static readonly EffectfvRefDelegate _EffectfvRef = LoadDelegate<EffectfvRefDelegate>("alEffectfv");

    /// <summary>
    /// Sets the vector value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Effect(int effect, EffectVector3 param, float[] value) => _EffectfvArray(effect, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void EffectfvArrayDelegate(int effect, EffectVector3 param, float[] value);
    static readonly EffectfvArrayDelegate _EffectfvArray = LoadDelegate<EffectfvArrayDelegate>("alEffectfv");

    /// <summary>
    /// Gets the value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetEffect(int effect, EffectInteger param, int* value) => _GetEffectiPtr(effect, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetEffectiPtrDelegate(int effect, EffectInteger param, int* value);
    static readonly GetEffectiPtrDelegate _GetEffectiPtr = LoadDelegate<GetEffectiPtrDelegate>("alGetEffecti");

    /// <summary>
    /// Gets the value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetEffect(int effect, EffectInteger param, out int value) => _GetEffectiRef(effect, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetEffectiRefDelegate(int effect, EffectInteger param, out int value);
    static readonly GetEffectiRefDelegate _GetEffectiRef = LoadDelegate<GetEffectiRefDelegate>("alGetEffecti");

    /// <summary>
    /// Gets the value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetEffect(int effect, EffectFloat param, float* value) => _GetEffectfPtr(effect, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetEffectfPtrDelegate(int effect, EffectFloat param, float* value);
    static readonly GetEffectfPtrDelegate _GetEffectfPtr = LoadDelegate<GetEffectfPtrDelegate>("alGetEffectf");

    /// <summary>
    /// Gets the value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetEffect(int effect, EffectFloat param, out float value) => _GetEffectfRef(effect, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetEffectfRefDelegate(int effect, EffectFloat param, out float value);
    static readonly GetEffectfRefDelegate _GetEffectfRef = LoadDelegate<GetEffectfRefDelegate>("alGetEffectf");

    /// <summary>
    /// Gets the vector value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetEffect(int effect, EffectVector3 param, float* value) => _GetEffectfvPtr(effect, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetEffectfvPtrDelegate(int effect, EffectVector3 param, float* value);
    static readonly GetEffectfvPtrDelegate _GetEffectfvPtr = LoadDelegate<GetEffectfvPtrDelegate>("alGetEffectfv");

    /// <summary>
    /// Gets the vector value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetEffect(int effect, EffectVector3 param, out float value) => _GetEffectfvRef(effect, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetEffectfvRefDelegate(int effect, EffectVector3 param, out float value);
    static readonly GetEffectfvRefDelegate _GetEffectfvRef = LoadDelegate<GetEffectfvRefDelegate>("alGetEffectfv");

    /// <summary>
    /// Creates one or more filter objects.
    /// </summary>
    /// <param name="count">The number of objects to generate.</param>
    /// <param name="filters">A pointer to the first element of the array where the handles will be stored.</param>
    /// <seealso cref="DeleteFilters(int, int*)"/>
    /// <seealso cref="IsFilter(int)"/>
    public static unsafe void GenFilters(int count, int* filters) => _GenFiltersPtr(count, filters);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GenFiltersPtrDelegate(int count, int* filters);
    static readonly GenFiltersPtrDelegate _GenFiltersPtr = LoadDelegate<GenFiltersPtrDelegate>("alGenFilters");

    /// <summary>
    /// Creates one or more filter objects.
    /// </summary>
    /// <param name="count">The number of objects to generate.</param>
    /// <param name="filters">A pointer to the first element of the array where the handles will be stored.</param>
    /// <seealso cref="DeleteFilters(int, ref int)"/>
    /// <seealso cref="IsFilter(int)"/>
    public static void GenFilters(int count, ref int filters) => _GenFiltersRef(count, ref filters);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GenFiltersRefDelegate(int count, ref int filters);
    static readonly GenFiltersRefDelegate _GenFiltersRef = LoadDelegate<GenFiltersRefDelegate>("alGenFilters");

    /// <summary>
    /// Creates one or more filter objects.
    /// </summary>
    /// <param name="count">The number of objects to generate.</param>
    /// <param name="filters">A pointer to the first element of the array where the handles will be stored.</param>
    /// <seealso cref="DeleteFilters(int, int[])"/>
    /// <seealso cref="IsFilter(int)"/>
    public static void GenFilters(int count, int[] filters) => _GenFiltersArray(count, filters);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GenFiltersArrayDelegate(int count, int[] filters);
    static readonly GenFiltersArrayDelegate _GenFiltersArray = LoadDelegate<GenFiltersArrayDelegate>("alGenFilters");

    /// <summary>
    /// Deletes one or more filter objects, freeing their resources.
    /// </summary>
    /// <param name="count">The number of objects to delete.</param>
    /// <param name="filters">A pointer to the first element of the array where the handles are stored.</param>
    /// <seealso cref="GenFilters(int, int*)"/>
    /// <seealso cref="IsFilter(int)"/>
    public static unsafe void DeleteFilters(int count, int* filters) => _DeleteFiltersPtr(count, filters);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void DeleteFiltersPtrDelegate(int count, int* filters);
    static readonly DeleteFiltersPtrDelegate _DeleteFiltersPtr = LoadDelegate<DeleteFiltersPtrDelegate>("alDeleteFilters");

    /// <summary>
    /// Deletes one or more filter objects, freeing their resources.
    /// </summary>
    /// <param name="count">The number of objects to delete.</param>
    /// <param name="filters">A pointer to the first element of the array where the handles are stored.</param>
    /// <seealso cref="GenFilters(int, ref int)"/>
    /// <seealso cref="IsFilter(int)"/>
    public static void DeleteFilters(int count, ref int filters) => _DeleteFiltersRef(count, ref filters);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void DeleteFiltersRefDelegate(int count, ref int filters);
    static readonly DeleteFiltersRefDelegate _DeleteFiltersRef = LoadDelegate<DeleteFiltersRefDelegate>("alDeleteFilters");

    /// <summary>
    /// Deletes one or more filter objects, freeing their resources.
    /// </summary>
    /// <param name="count">The number of objects to delete.</param>
    /// <param name="filters">A pointer to the first element of the array where the handles are stored.</param>
    /// <seealso cref="GenFilters(int, int[])"/>
    /// <seealso cref="IsFilter(int)"/>
    public static void DeleteFilters(int count, int[] filters) => _DeleteFiltersArray(count, filters);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void DeleteFiltersArrayDelegate(int count, int[] filters);
    static readonly DeleteFiltersArrayDelegate _DeleteFiltersArray = LoadDelegate<DeleteFiltersArrayDelegate>("alDeleteFilters");

    /// <summary>
    /// Determines whether or not a given handle is an filter handle.
    /// </summary>
    /// <param name="filter">The handle.</param>
    /// <returns>true if the handle is an filter handle; otherwise, false.</returns>
    /// <seealso cref="GenFilters(int)"/>
    /// <seealso cref="DeleteFilters(int[])"/>
    public static bool IsFilter(int filter) => _IsFilter(filter);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate bool IsFilterDelegate(int filter);
    static readonly IsFilterDelegate _IsFilter = LoadDelegate<IsFilterDelegate>("alIsFilter");

    /// <summary>
    /// Sets the value of a named property on the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Filter(int filter, FilterInteger param, int value) => _Filteri(filter, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void FilteriDelegate(int filter, FilterInteger param, int value);
    static readonly FilteriDelegate _Filteri = LoadDelegate<FilteriDelegate>("alFilteri");

    /// <summary>
    /// Sets the value of a named property on the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Filter(int filter, FilterFloat param, float value) => _Filterf(filter, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void FilterfDelegate(int filter, FilterFloat param, float value);
    static readonly FilterfDelegate _Filterf = LoadDelegate<FilterfDelegate>("alFilterf");

    /// <summary>
    /// Gets the value of a named property on the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetFilter(int filter, FilterInteger param, int* value) => _GetFilteriPtr(filter, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetFilteriPtrDelegate(int filter, FilterInteger param, int* value);
    static readonly GetFilteriPtrDelegate _GetFilteriPtr = LoadDelegate<GetFilteriPtrDelegate>("alGetFilteri");

    /// <summary>
    /// Gets the value of a named property on the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetFilter(int filter, FilterInteger param, out int value) => _GetFilteriRef(filter, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetFilteriRefDelegate(int filter, FilterInteger param, out int value);
    static readonly GetFilteriRefDelegate _GetFilteriRef = LoadDelegate<GetFilteriRefDelegate>("alGetFilteri");

    /// <summary>
    /// Gets the value of a named property on the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetFilter(int filter, FilterFloat param, float* value) => _GetFilterfPtr(filter, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetFilterfPtrDelegate(int filter, FilterFloat param, float* value);
    static readonly GetFilterfPtrDelegate _GetFilterfPtr = LoadDelegate<GetFilterfPtrDelegate>("alGetFilterf");

    /// <summary>
    /// Gets the value of a named property on the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetFilter(int filter, FilterFloat param, out float value) => _GetFilterfRef(filter, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetFilterfRefDelegate(int filter, FilterFloat param, out float value);
    static readonly GetFilterfRefDelegate _GetFilterfRef = LoadDelegate<GetFilterfRefDelegate>("alGetFilterf");

    /// <summary>
    /// Sets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Source(int source, EFXSourceInteger param, int value) => _Sourcei(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void SourceiDelegate(int source, EFXSourceInteger param, int value);
    static readonly SourceiDelegate _Sourcei = LoadDelegate<SourceiDelegate>("alSourcei");

    /// <summary>
    /// Sets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Source(int source, EFXSourceFloat param, float value) => _Source(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void SourceDelegate(int source, EFXSourceFloat param, float value);
    static readonly SourceDelegate _Source = LoadDelegate<SourceDelegate>("alSourcei");

    /// <summary>
    /// Sets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Source(int source, EFXSourceBoolean param, bool value) => _Sourceb(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void SourcebDelegate(int source, EFXSourceBoolean param, bool value);
    static readonly SourcebDelegate _Sourceb = LoadDelegate<SourcebDelegate>("alSourcei");

    /// <summary>
    /// Sets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void Source(int source, EFXSourceInteger3 param, int* value) => _SourceivPtr(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void SourceivPtrDelegate(int source, EFXSourceInteger3 param, int* value);
    static readonly SourceivPtrDelegate _SourceivPtr = LoadDelegate<SourceivPtrDelegate>("alSourceiv");

    /// <summary>
    /// Sets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Source(int source, EFXSourceInteger3 param, ref int value) => _SourceivRef(source, param, ref value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void SourceivRefDelegate(int source, EFXSourceInteger3 param, ref int value);
    static readonly SourceivRefDelegate _SourceivRef = LoadDelegate<SourceivRefDelegate>("alSourceiv");

    /// <summary>
    /// Sets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Source(int source, EFXSourceInteger3 param, int[] value) => _SourceivArray(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void SourceivArrayDelegate(int source, EFXSourceInteger3 param, int[] value);
    static readonly SourceivArrayDelegate _SourceivArray = LoadDelegate<SourceivArrayDelegate>("alSourceiv");

    /// <summary>
    /// Sets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value1">The first value to set the property to.</param>
    /// <param name="value2">The second value to set the property to.</param>
    /// <param name="value3">The third value to set the property to.</param>
    public static void Source(int source, EFXSourceInteger3 param, int value1, int value2, int value3) => _Source3i(source, param, value1, value2, value3);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void Source3iDelegate(int source, EFXSourceInteger3 param, int value1, int value2, int value3);
    static readonly Source3iDelegate _Source3i = LoadDelegate<Source3iDelegate>("alSource3i");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetSource(int source, EFXSourceInteger param, int* value) => _GetSourceiPtr(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetSourceiPtrDelegate(int source, EFXSourceInteger param, int* value);
    static readonly GetSourceiPtrDelegate _GetSourceiPtr = LoadDelegate<GetSourceiPtrDelegate>("alGetSourcei");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetSource(int source, EFXSourceInteger param, out int value) => _GetSourceiRef(source, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSourceiRefDelegate(int source, EFXSourceInteger param, out int value);
    static readonly GetSourceiRefDelegate _GetSourceiRef = LoadDelegate<GetSourceiRefDelegate>("alGetSourcei");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetSource(int source, EFXSourceFloat param, float* value) => _GetSourcefPtr(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetSourcefPtrDelegate(int source, EFXSourceFloat param, float* value);
    static readonly GetSourcefPtrDelegate _GetSourcefPtr = LoadDelegate<GetSourcefPtrDelegate>("alGetSourcef");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetSource(int source, EFXSourceFloat param, out float value) => _GetSourcefRef(source, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSourcefRefDelegate(int source, EFXSourceFloat param, out float value);
    static readonly GetSourcefRefDelegate _GetSourcefRef = LoadDelegate<GetSourcefRefDelegate>("alGetSourcef");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetSource(int source, EFXSourceBoolean param, bool* value) => _GetSourcebPtr(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetSourcebPtrDelegate(int source, EFXSourceBoolean param, bool* value);
    static readonly GetSourcebPtrDelegate _GetSourcebPtr = LoadDelegate<GetSourcebPtrDelegate>("alGetSourcei");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetSource(int source, EFXSourceBoolean param, out bool value) => _GetSourcebRef(source, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSourcebRefDelegate(int source, EFXSourceBoolean param, out bool value);
    static readonly GetSourcebRefDelegate _GetSourcebRef = LoadDelegate<GetSourcebRefDelegate>("alGetSourcei");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetSource(int source, EFXSourceInteger3 param, int* value) => _GetSourceivPtr(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetSourceivPtrDelegate(int source, EFXSourceInteger3 param, int* value);
    static readonly GetSourceivPtrDelegate _GetSourceivPtr = LoadDelegate<GetSourceivPtrDelegate>("alGetSourceiv");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetSource(int source, EFXSourceInteger3 param, ref int value) => _GetSourceivRef(source, param, ref value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSourceivRefDelegate(int source, EFXSourceInteger3 param, ref int value);
    static readonly GetSourceivRefDelegate _GetSourceivRef = LoadDelegate<GetSourceivRefDelegate>("alGetSourceiv");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetSource(int source, EFXSourceInteger3 param, int[] value) => _GetSourceivArray(source, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSourceivArrayDelegate(int source, EFXSourceInteger3 param, int[] value);
    static readonly GetSourceivArrayDelegate _GetSourceivArray = LoadDelegate<GetSourceivArrayDelegate>("alGetSourceiv");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value1">The first value to set the property to.</param>
    /// <param name="value2">The second value to set the property to.</param>
    /// <param name="value3">The third value to set the property to.</param>
    public static unsafe void GetSource(int source, EFXSourceInteger3 param, int* value1, int* value2, int* value3) => _GetSource3iPtr(source, param, value1, value2, value3);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetSource3iPtrDelegate(int source, EFXSourceInteger3 param, int* value1, int* value2, int* value3);
    static readonly GetSource3iPtrDelegate _GetSource3iPtr = LoadDelegate<GetSource3iPtrDelegate>("alGetSource3i");

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value1">The first value to set the property to.</param>
    /// <param name="value2">The second value to set the property to.</param>
    /// <param name="value3">The third value to set the property to.</param>
    public static void GetSource(int source, EFXSourceInteger3 param, out int value1, out int value2, out int value3) => _GetSource3iRef(source, param, out value1, out value2, out value3);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetSource3iRefDelegate(int source, EFXSourceInteger3 param, out int value1, out int value2, out int value3);
    static readonly GetSource3iRefDelegate _GetSource3iRef = LoadDelegate<GetSource3iRefDelegate>("alGetSource3i");

    /// <summary>
    /// Sets the value of a named property on the given listener.
    /// </summary>
    /// <param name="listener">The listener.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Listener(int listener, EFXListenerFloat param, float value) => _Listenerf(listener, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void ListenerfDelegate(int listener, EFXListenerFloat param, float value);
    static readonly ListenerfDelegate _Listenerf = LoadDelegate<ListenerfDelegate>("alListenerf");

    /// <summary>
    /// Gets the value of a named property on the given listener.
    /// </summary>
    /// <param name="listener">The listener.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static unsafe void GetListener(int listener, EFXListenerFloat param, float* value) => _GetListenerfPtr(listener, param, value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public unsafe delegate void GetListenerfPtrDelegate(int listener, EFXListenerFloat param, float* value);
    static readonly GetListenerfPtrDelegate _GetListenerfPtr = LoadDelegate<GetListenerfPtrDelegate>("alGetListenerf");

    /// <summary>
    /// Gets the value of a named property on the given listener.
    /// </summary>
    /// <param name="listener">The listener.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetListener(int listener, EFXListenerFloat param, out float value) => _GetListenerfRef(listener, param, out value);
    [UnmanagedFunctionPointer(AL.ALCallingConvention)] public delegate void GetListenerfRefDelegate(int listener, EFXListenerFloat param, out float value);
    static readonly GetListenerfRefDelegate _GetListenerfRef = LoadDelegate<GetListenerfRefDelegate>("alGetListenerf");

    #endregion

    /// <summary>
    /// Creates one or more auxiliary effect slots.
    /// </summary>
    /// <param name="slots">An array to fill with created slots.</param>
    /// <seealso cref="DeleteAuxiliaryEffectSlots(int[])"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static void GenAuxiliaryEffectSlots(int[] slots) => GenAuxiliaryEffectSlots(slots.Length, slots);

    /// <summary>
    /// Creates one or more auxiliary effect slots.
    /// </summary>
    /// <param name="count">The number of slots to create.</param>
    /// <returns>The slots.</returns>
    /// <seealso cref="DeleteAuxiliaryEffectSlots(int[])"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static int[] GenAuxiliaryEffectSlots(int count) { var result = new int[count]; GenAuxiliaryEffectSlots(count, result); return result; }

    /// <summary>
    /// Creates an auxiliary effect slot.
    /// </summary>
    /// <returns>The slot.</returns>
    /// <seealso cref="DeleteAuxiliaryEffectSlot"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static int GenAuxiliaryEffectSlot() { var result = 0; GenAuxiliaryEffectSlots(1, ref result); return result; }

    /// <summary>
    /// Creates an auxiliary effect slot.
    /// </summary>
    /// <param name="slot">The generated slot.</param>
    /// <seealso cref="DeleteAuxiliaryEffectSlot"/>
    /// <seealso cref="IsAuxiliaryEffectSlot"/>
    public static void GenAuxiliaryEffectSlot(out int slot) { var result = 0; GenAuxiliaryEffectSlots(1, ref result); slot = result; }

    /// <summary>
    /// Deletes and frees resources used for a set of auxiliary effect slots.
    /// </summary>
    /// <param name="slots">An array of slots to delete.</param>
    /// <seealso cref="GenAuxiliaryEffectSlots(int)"/>
    /// <seealso cref="IsAuxiliaryEffectSlot(int)"/>
    public static void DeleteAuxiliaryEffectSlots(int[] slots) => DeleteAuxiliaryEffectSlots(slots.Length, slots);

    /// <summary>
    /// Deletes and frees resources used an auxiliary effect slot.
    /// </summary>
    /// <param name="slot">The slot to delete.</param>
    /// <seealso cref="GenAuxiliaryEffectSlot()"/>
    /// <seealso cref="IsAuxiliaryEffectSlot(int)"/>
    public static void DeleteAuxiliaryEffectSlot(int slot) => DeleteAuxiliaryEffectSlots(1, ref slot);

    /// <summary>
    /// Gets the value of a named property on the given effect slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    public static int GetAuxiliaryEffectSlot(int slot, EffectSlotInteger param) { GetAuxiliaryEffectSlot(slot, param, out var result); return result; }

    /// <summary>
    /// Gets the value of a named property on the given effect slot.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    public static float GetAuxiliaryEffectSlot(int slot, EffectSlotFloat param) { GetAuxiliaryEffectSlot(slot, param, out var result); return result; }

    /// <summary>
    /// Creates one or more effects.
    /// </summary>
    /// <param name="effects">An arrays to fill with the generated effects.</param>
    /// <seealso cref="DeleteEffects(int[])"/>
    /// <seealso cref="IsEffect"/>
    public static void GenEffects(int[] effects) => GenEffects(effects.Length, effects);

    /// <summary>
    /// Creates one or more effects.
    /// </summary>
    /// <param name="count">The number of effects to create.</param>
    /// <returns>The effects.</returns>
    /// <seealso cref="DeleteEffects(int[])"/>
    /// <seealso cref="IsEffect"/>
    public static int[] GenEffects(int count) { var result = new int[count]; GenEffects(count, result); return result; }

    /// <summary>
    /// Creates an effect.
    /// </summary>
    /// <returns>The effect.</returns>
    /// <seealso cref="DeleteEffect"/>
    /// <seealso cref="IsEffect"/>
    public static int GenEffect() { var result = 0; GenEffects(1, ref result); return result; }

    /// <summary>
    /// Creates an effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <seealso cref="DeleteEffect"/>
    /// <seealso cref="IsEffect"/>
    public static void GenEffect(out int effect) { var result = 0; GenEffects(1, ref result); effect = result; }

    /// <summary>
    /// Deletes and frees resources used for a set of effects.
    /// </summary>
    /// <param name="effects">An array of effects to delete.</param>
    /// <seealso cref="GenEffects(int)"/>
    /// <seealso cref="IsEffect"/>
    public static void DeleteEffects(int[] effects) => DeleteEffects(effects.Length, effects);

    /// <summary>
    /// Deletes and frees resources used an effect.
    /// </summary>
    /// <param name="effect">The effect to delete.</param>
    /// <seealso cref="GenEffect()"/>
    /// <seealso cref="IsEffect"/>
    public static void DeleteEffect(int effect) => DeleteEffects(1, ref effect);

    /// <summary>
    /// Sets the vector value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void Effect(int effect, EffectVector3 param, ref Vector3 value) => Effect(effect, param, ref value.X);

    /// <summary>
    /// Gets the value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    public static float GetEffect(int effect, EffectFloat param) { GetEffect(effect, param, out float result); return result; }

    /// <summary>
    /// Gets the vector value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <param name="value">The value to set the property to.</param>
    public static void GetEffect(int effect, EffectVector3 param, out Vector3 value)
    {
        // This is so the compiler won't complain
        value.Y = 0;
        value.Z = 0;
        // This will fill the whole struct, not just the x field.
        GetEffect(effect, param, out value.X);
    }

    /// <summary>
    /// Gets the value of a named property on the given effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    public static Vector3 GetEffect(int effect, EffectVector3 param) { GetEffect(effect, param, out Vector3 result); return result; }

    /// <summary>
    /// Creates one or more filters.
    /// </summary>
    /// <param name="filters">An array to fill with the generated filters.</param>
    /// <seealso cref="DeleteFilters(int[])"/>
    /// <seealso cref="IsFilter"/>
    public static void GenFilters(int[] filters) => GenFilters(filters.Length, filters);

    /// <summary>
    /// Creates one or more filters.
    /// </summary>
    /// <param name="count">The number of filters to create.</param>
    /// <returns>The filters.</returns>
    /// <seealso cref="DeleteFilters(int[])"/>
    /// <seealso cref="IsFilter"/>
    public static int[] GenFilters(int count) { var result = new int[count]; GenFilters(count, result); return result; }

    /// <summary>
    /// Creates an filter.
    /// </summary>
    /// <returns>The filter.</returns>
    /// <seealso cref="DeleteFilter(int)"/>
    /// <seealso cref="IsFilter"/>
    public static int GenFilter() { var result = 0; GenFilters(1, ref result); return result; }

    /// <summary>
    /// Creates an filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <seealso cref="DeleteFilter(int)"/>
    /// <seealso cref="IsFilter"/>
    public static void GenFilter(out int filter) { var result = 0; GenFilters(1, ref result); filter = result; }

    /// <summary>
    /// Deletes and frees resources used for a set of filters.
    /// </summary>
    /// <param name="filters">An array of filters to delete.</param>
    /// <seealso cref="GenFilters(int)"/>
    /// <seealso cref="IsFilter"/>
    public static void DeleteFilters(int[] filters) => DeleteFilters(filters.Length, filters);

    /// <summary>
    /// Deletes and frees resources used an filter.
    /// </summary>
    /// <param name="filter">The filter to delete.</param>
    /// <seealso cref="GenFilter()"/>
    /// <seealso cref="IsFilter"/>
    public static void DeleteFilter(int filter) => DeleteFilters(1, ref filter);

    /// <summary>
    /// Gets the value of a named property on the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    public static int GetFilter(int filter, FilterInteger param) { GetFilter(filter, param, out int result); return result; }

    /// <summary>
    /// Gets the value of a named property on the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    public static float GetFilter(int filter, FilterFloat param) { GetFilter(filter, param, out float result); return result; }

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    public static int GetSource(int source, EFXSourceInteger param) { GetSource(source, param, out int result); return result; }

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    public static float GetSource(int source, EFXSourceFloat param) { GetSource(source, param, out float result); return result; }

    /// <summary>
    /// Gets the value of a named property on the given source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value.</returns>
    public static bool GetSource(int source, EFXSourceBoolean param) { GetSource(source, param, out bool result); return result; }

    /// <summary>
    /// Gets the value of a named property on the given listener.
    /// </summary>
    /// <param name="listener">The listener.</param>
    /// <param name="param">The named property.</param>
    /// <returns>The value of the property.</returns>
    public static float GetListener(int listener, EFXListenerFloat param) { GetListener(listener, param, out float result); return result; }
}

#region Enums

/// <summary>
/// A list of valid 32-bit Float Effect/GetEffect parameters.
/// </summary>
public enum EffectFloat
{
    /// <summary>
    /// Reverb Modal Density controls the coloration of the late reverb. Lowering the value adds more coloration to
    /// the late reverb. Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    ReverbDensity = 0x0001,
    /// <summary>
    /// The Reverb Diffusion property controls the echo density in the reverberation decay. The default 1.0f provides
    /// the highest density. Reducing diffusion gives the reverberation a more "grainy" character that is especially
    /// noticeable with percussive sound sources. If you set a diffusion value of 0.0f, the later reverberation sounds like
    /// a succession of distinct echoes. Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    ReverbDiffusion = 0x0002,
    /// <summary>
    /// The Reverb Gain property is the master volume control for the reflected sound - both early reflections and
    /// reverberation - that the reverb effect adds to all sound sources. Ranges from 1.0 (0db) (the maximum amount) to 0.0
    /// (-100db) (no reflected sound at all) are accepted. Units: Linear gain Range [0.0f .. 1.0f] Default: 0.32f
    /// </summary>
    ReverbGain = 0x0003,
    /// <summary>
    /// The Reverb Gain HF property further tweaks reflected sound by attenuating it at high frequencies. It controls
    /// a low-pass filter that applies globally to the reflected sound of all sound sources feeding the particular instance
    /// of the reverb effect. Ranges from 1.0f (0db) (no filter) to 0.0f (-100db) (virtually no reflected sound) are
    /// accepted. Units: Linear gain Range [0.0f .. 1.0f] Default: 0.89f
    /// </summary>
    ReverbGainHF = 0x0004,
    /// <summary>
    /// The Decay Time property sets the reverberation decay time. It ranges from 0.1f (typically a small room with
    /// very dead surfaces) to 20.0 (typically a large room with very live surfaces). Unit: Seconds Range [0.1f .. 20.0f]
    /// Default: 1.49f
    /// </summary>
    ReverbDecayTime = 0x0005,
    /// <summary>
    /// The Decay HF Ratio property sets the spectral quality of the Decay Time parameter. It is the ratio of
    /// high-frequency decay time relative to the time set by Decay Time.. Unit: linear multiplier Range [0.1f .. 2.0f]
    /// Default: 0.83f
    /// </summary>
    ReverbDecayHFRatio = 0x0006,
    /// <summary>
    /// The Reflections Gain property controls the overall amount of initial reflections relative to the Gain
    /// property. The value of Reflections Gain ranges from a maximum of 3.16f (+10 dB) to a minimum of 0.0f (-100 dB) (no
    /// initial reflections at all), and is corrected by the value of the Gain property. Unit: Linear gain Range [0.0f ..
    /// 3.16f] Default: 0.05f
    /// </summary>
    ReverbReflectionsGain = 0x0007,
    /// <summary>
    /// The Reflections Delay property is the amount of delay between the arrival time of the direct path from the
    /// source to the first reflection from the source. It ranges from 0 to 300 milliseconds. Unit: Seconds Range [0.0f ..
    /// 0.3f] Default: 0.007f
    /// </summary>
    ReverbReflectionsDelay = 0x0008,
    /// <summary>
    /// The Late Reverb Gain property controls the overall amount of later reverberation relative to the Gain
    /// property. The value of Late Reverb Gain ranges from a maximum of 10.0f (+20 dB) to a minimum of 0.0f (-100 dB) (no
    /// late reverberation at all). Unit: Linear gain Range [0.0f .. 10.0f] Default: 1.26f
    /// </summary>
    ReverbLateReverbGain = 0x0009,
    /// <summary>
    /// The Late Reverb Delay property defines the begin time of the late reverberation relative to the time of the
    /// initial reflection (the first of the early reflections). It ranges from 0 to 100 milliseconds. Unit: Seconds Range
    /// [0.0f .. 0.1f] Default: 0.011f
    /// </summary>
    ReverbLateReverbDelay = 0x000A,
    /// <summary>
    /// The Air Absorption Gain HF property controls the distance-dependent attenuation at high frequencies caused by
    /// the propagation medium and applies to reflected sound only. Unit: Linear gain per meter Range [0.892f .. 1.0f]
    /// Default: 0.994f
    /// </summary>
    ReverbAirAbsorptionGainHF = 0x000B,
    /// <summary>
    /// The Room Rolloff Factor property is one of two methods available to attenuate the reflected sound (containing
    /// both reflections and reverberation) according to source-listener distance. It's defined the same way as OpenAL's
    /// Rolloff Factor, but operates on reverb sound instead of direct-path sound. Unit: Linear multiplier Range [0.0f ..
    /// 10.0f] Default: 0.0f
    /// </summary>
    ReverbRoomRolloffFactor = 0x000C,
    /// <summary>
    /// This property sets the modulation rate of the low-frequency oscillator that controls the delay time of the
    /// delayed signals. Unit: Hz Range [0.0f .. 10.0f] Default: 1.1f
    /// </summary>
    ChorusRate = 0x0003,
    /// <summary>
    /// This property controls the amount by which the delay time is modulated by the low-frequency oscillator. Range
    /// [0.0f .. 1.0f] Default: 0.1f
    /// </summary>
    ChorusDepth = 0x0004,
    /// <summary>
    /// This property controls the amount of processed signal that is fed back to the input of the chorus effect.
    /// Negative values will reverse the phase of the feedback signal. At full magnitude the identical sample will repeat
    /// endlessly. Range [-1.0f .. +1.0f] Default: +0.25f
    /// </summary>
    ChorusFeedback = 0x0005,
    /// <summary>
    /// This property controls the average amount of time the sample is delayed before it is played back, and with
    /// feedback, the amount of time between iterations of the sample. Larger values lower the pitch. Unit: Seconds Range
    /// [0.0f .. 0.016f] Default: 0.016f
    /// </summary>
    ChorusDelay = 0x0006,
    /// <summary>
    /// This property controls the shape of the distortion. The higher the value for Edge, the "dirtier" and "fuzzier"
    /// the effect. Range [0.0f .. 1.0f] Default: 0.2f
    /// </summary>
    DistortionEdge = 0x0001,
    /// <summary>
    /// This property allows you to attenuate the distorted sound. Range [0.01f .. 1.0f] Default: 0.05f
    /// </summary>
    DistortionGain = 0x0002,
    /// <summary>
    /// Input signals can have a low pass filter applied, to limit the amount of high frequency signal feeding into
    /// the distortion effect. Unit: Hz Range [80.0f .. 24000.0f] Default: 8000.0f
    /// </summary>
    DistortionLowpassCutoff = 0x0003,
    /// <summary>
    /// This property controls the frequency at which the post-distortion attenuation (Distortion Gain) is active.
    /// Unit: Hz Range [80.0f .. 24000.0f] Default: 3600.0f
    /// </summary>
    DistortionEQCenter = 0x0004,
    /// <summary>
    /// This property controls the bandwidth of the post-distortion attenuation. Unit: Hz Range [80.0f .. 24000.0f]
    /// Default: 3600.0f
    /// </summary>
    DistortionEQBandwidth = 0x0005,
    /// <summary>
    /// This property controls the delay between the original sound and the first "tap", or echo instance.
    /// Subsequently, the value for Echo Delay is used to determine the time delay between each "second tap" and the next
    /// "first tap". Unit: Seconds Range [0.0f .. 0.207f] Default: 0.1f
    /// </summary>
    EchoDelay = 0x0001,
    /// <summary>
    /// This property controls the delay between the "first tap" and the "second tap". Subsequently, the value for
    /// Echo LR Delay is used to determine the time delay between each "first tap" and the next "second tap". Unit: Seconds
    /// Range [0.0f .. 0.404f] Default: 0.1f
    /// </summary>
    EchoLRDelay = 0x0002,
    /// <summary>
    /// This property controls the amount of high frequency damping applied to each echo. As the sound is subsequently
    /// fed back for further echoes, damping results in an echo which progressively gets softer in tone as well as
    /// intensity. Range [0.0f .. 0.99f] Default: 0.5f
    /// </summary>
    EchoDamping = 0x0003,
    /// <summary>
    /// This property controls the amount of feedback the output signal fed back into the input. Use this parameter to
    /// create "cascading" echoes. At full magnitude, the identical sample will repeat endlessly. Below full magnitude, the
    /// sample will repeat and fade. Range [0.0f .. 1.0f] Default: 0.5f
    /// </summary>
    EchoFeedback = 0x0004,
    /// <summary>
    /// This property controls how hard panned the individual echoes are. With a value of 1.0f, the first "tap" will
    /// be panned hard left, and the second "tap" hard right. –1.0f gives the opposite result and values near to 0.0f
    /// result in less emphasized panning. Range [-1.0f .. +1.0f] Default: -1.0f
    /// </summary>
    EchoSpread = 0x0005,
    /// <summary>
    /// The number of times per second the low-frequency oscillator controlling the amount of delay repeats. Range
    /// [0.0f .. 10.0f] Default: 0.27f
    /// </summary>
    FlangerRate = 0x0003,
    /// <summary>
    /// The ratio by which the delay time is modulated by the low-frequency oscillator. Range [0.0f .. 1.0f] Default:
    /// 1.0f
    /// </summary>
    FlangerDepth = 0x0004,
    /// <summary>
    /// This is the amount of the output signal level fed back into the effect's input. A negative value will reverse
    /// the phase of the feedback signal. Range [-1.0f .. +1.0f] Default: -0.5f
    /// </summary>
    FlangerFeedback = 0x0005,
    /// <summary>
    /// The average amount of time the sample is delayed before it is played back. When used with the Feedback
    /// property it's the amount of time between iterations of the sample. Unit: Seconds Range [0.0f .. 0.004f] Default:
    /// 0.002f
    /// </summary>
    FlangerDelay = 0x0006,
    /// <summary>
    /// This is the carrier frequency. For carrier frequencies below the audible range, the single sideband modulator
    /// may produce phaser effects, spatial effects or a slight pitch-shift. As the carrier frequency increases, the timbre
    /// of the sound is affected. Unit: Hz Range [0.0f .. 24000.0f] Default: 0.0f
    /// </summary>
    FrequencyShifterFrequency = 0x0001,
    /// <summary>
    /// This controls the frequency of the low-frequency oscillator used to morph between the two phoneme filters.
    /// Unit: Hz Range [0.0f .. 10.0f] Default: 1.41f
    /// </summary>
    VocalMorpherRate = 0x0006,
    /// <summary>
    /// This is the frequency of the carrier signal. If the carrier signal is slowly varying (less than 20 Hz), the
    /// result is a slow amplitude variation effect (tremolo). Unit: Hz Range [0.0f .. 8000.0f] Default: 440.0f
    /// </summary>
    RingModulatorFrequency = 0x0001,
    /// <summary>
    /// This controls the cutoff frequency at which the input signal is high-pass filtered before being ring
    /// modulated. Unit: Hz Range [0.0f .. 24000.0f] Default: 800.0f
    /// </summary>
    RingModulatorHighpassCutoff = 0x0002,
    /// <summary>
    /// This property controls the time the filtering effect takes to sweep from minimum to maximum center frequency
    /// when it is triggered by input signal. Unit: Seconds Range [0.0001f .. 1.0f] Default: 0.06f
    /// </summary>
    AutowahAttackTime = 0x0001,
    /// <summary>
    /// This property controls the time the filtering effect takes to sweep from maximum back to base center
    /// frequency, when the input signal ends. Unit: Seconds Range [0.0001f .. 1.0f] Default: 0.06f
    /// </summary>
    AutowahReleaseTime = 0x0002,
    /// <summary>
    /// This property controls the resonant peak, sometimes known as emphasis or Q, of the auto-wah band-pass filter.
    /// Range [2.0f .. 1000.0f] Default: 1000.0f
    /// </summary>
    AutowahResonance = 0x0003,
    /// <summary>
    /// This property controls the input signal level at which the band-pass filter will be fully opened. Range
    /// [0.00003f .. 31621.0f] Default: 11.22f
    /// </summary>
    AutowahPeakGain = 0x0004,
    /// <summary>
    /// This property controls amount of cut or boost on the low frequency range. Range [0.126f .. 7.943f] Default:
    /// 1.0f
    /// </summary>
    EqualizerLowGain = 0x0001,
    /// <summary>
    /// This property controls the low frequency below which signal will be cut off. Unit: Hz Range [50.0f .. 800.0f]
    /// Default: 200.0f
    /// </summary>
    EqualizerLowCutoff = 0x0002,
    /// <summary>
    /// This property allows you to cut/boost signal on the "mid1" range. Range [0.126f .. 7.943f] Default: 1.0f
    /// </summary>
    EqualizerMid1Gain = 0x0003,
    /// <summary>
    /// This property sets the center frequency for the "mid1" range. Unit: Hz Range [200.0f .. 3000.0f] Default:
    /// 500.0f
    /// </summary>
    EqualizerMid1Center = 0x0004,
    /// <summary>
    /// This property controls the width of the "mid1" range. Range [0.01f .. 1.0f] Default: 1.0f
    /// </summary>
    EqualizerMid1Width = 0x0005,
    /// <summary>
    /// This property allows you to cut/boost signal on the "mid2" range. Range [0.126f .. 7.943f] Default: 1.0f
    /// </summary>
    EqualizerMid2Gain = 0x0006,
    /// <summary>
    /// This property sets the center frequency for the "mid2" range. Unit: Hz Range [1000.0f .. 8000.0f] Default:
    /// 3000.0f
    /// </summary>
    EqualizerMid2Center = 0x0007,
    /// <summary>
    /// This property controls the width of the "mid2" range. Range [0.01f .. 1.0f] Default: 1.0f
    /// </summary>
    EqualizerMid2Width = 0x0008,
    /// <summary>
    /// This property allows to cut/boost the signal at high frequencies. Range [0.126f .. 7.943f] Default: 1.0f
    /// </summary>
    EqualizerHighGain = 0x0009,
    /// <summary>
    /// This property controls the high frequency above which signal will be cut off. Unit: Hz Range [4000.0f ..
    /// 16000.0f] Default: 6000.0f
    /// </summary>
    EqualizerHighCutoff = 0x000A,
    /// <summary>
    /// Reverb Modal Density controls the coloration of the late reverb. Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    EaxReverbDensity = 0x0001,
    /// <summary>
    /// The Reverb Diffusion property controls the echo density in the reverberation decay. Range [0.0f .. 1.0f]
    /// Default: 1.0f
    /// </summary>
    EaxReverbDiffusion = 0x0002,
    /// <summary>
    /// Reverb Gain controls the level of the reverberant sound in an environment. A high level of reverb is
    /// characteristic of rooms with highly reflective walls and/or small dimensions. Unit: Linear gain Range [0.0f ..
    /// 1.0f] Default: 0.32f
    /// </summary>
    EaxReverbGain = 0x0003,
    /// <summary>
    /// Gain HF is used to attenuate the high frequency content of all the reflected sound in an environment. You can
    /// use this property to give a room specific spectral characteristic. Unit: Linear gain Range [0.0f .. 1.0f] Default:
    /// 0.89f
    /// </summary>
    EaxReverbGainHF = 0x0004,
    /// <summary>
    /// Gain LF is the low frequency counterpart to Gain HF. Use this to reduce or boost the low frequency content in
    /// an environment. Unit: Linear gain Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    EaxReverbGainLF = 0x0005,
    /// <summary>
    /// The Decay Time property sets the reverberation decay time. It ranges from 0.1f (typically a small room with
    /// very dead surfaces) to 20.0f (typically a large room with very live surfaces). Unit: Seconds Range [0.1f .. 20.0f]
    /// Default: 1.49f
    /// </summary>
    EaxReverbDecayTime = 0x0006,
    /// <summary>
    /// Decay HF Ratio scales the decay time of high frequencies relative to the value of the Decay Time property. By
    /// changing this value, you are changing the amount of time it takes for the high frequencies to decay compared to the
    /// mid frequencies of the reverb. Range [0.1f .. 2.0f] Default: 0.83f
    /// </summary>
    EaxReverbDecayHFRatio = 0x0007,
    /// <summary>
    /// Decay LF Ratio scales the decay time of low frequencies in the reverberation in the same manner that Decay HF
    /// Ratio handles high frequencies. Unit: Linear multiplier Range [0.1f .. 2.0f] Default: 1.0f
    /// </summary>
    EaxReverbDecayLFRatio = 0x0008,
    /// <summary>
    /// Reflections Gain sets the level of the early reflections in an environment. Early reflections are used as a
    /// cue for determining the size of the environment we are in. Unit: Linear gain Range [0.0f .. 3.16f] Default: 0.05f
    /// </summary>
    EaxReverbReflectionsGain = 0x0009,
    /// <summary>
    /// Reflections Delay controls the amount of time it takes for the first reflected wave front to reach the
    /// listener, relative to the arrival of the direct-path sound. Unit: Seconds Range [0.0f .. 0.3f] Default: 0.007f
    /// </summary>
    EaxReverbReflectionsDelay = 0x000A,
    /// <summary>
    /// The Late Reverb Gain property controls the overall amount of later reverberation relative to the Gain
    /// property. Range [0.0f .. 10.0f] Default: 1.26f
    /// </summary>
    EaxReverbLateReverbGain = 0x000C,
    /// <summary>
    /// The Late Reverb Delay property defines the begin time of the late reverberation relative to the time of the
    /// initial reflection (the first of the early reflections). It ranges from 0 to 100 milliseconds. Unit: Seconds Range
    /// [0.0f .. 0.1f] Default: 0.011f
    /// </summary>
    EaxReverbLateReverbDelay = 0x000D,
    /// <summary>
    /// Echo Time controls the rate at which the cyclic echo repeats itself along the reverberation decay. Range
    /// [0.075f .. 0.25f] Default: 0.25f
    /// </summary>
    EaxReverbEchoTime = 0x000F,
    /// <summary>
    /// Echo Depth introduces a cyclic echo in the reverberation decay, which will be noticeable with transient or
    /// percussive sounds. Range [0.0f .. 1.0f] Default: 0.0f
    /// </summary>
    EaxReverbEchoDepth = 0x0010,
    /// <summary>
    /// Modulation Time controls the speed of the rate of periodic changes in pitch (vibrato). Range [0.04f .. 4.0f]
    /// Default: 0.25f
    /// </summary>
    EaxReverbModulationTime = 0x0011,
    /// <summary>
    /// Modulation Depth controls the amount of pitch change. Low values of Diffusion will contribute to reinforcing
    /// the perceived effect by reducing the mixing of overlapping reflections in the reverberation decay. Range [0.0f ..
    /// 1.0f] Default: 0.0f
    /// </summary>
    EaxReverbModulationDepth = 0x0012,
    /// <summary>
    /// The Air Absorption Gain HF property controls the distance-dependent attenuation at high frequencies caused by
    /// the propagation medium. It applies to reflected sound only. Range [0.892f .. 1.0f] Default: 0.994f
    /// </summary>
    EaxReverbAirAbsorptionGainHF = 0x0013,
    /// <summary>
    /// The property HF reference determines the frequency at which the high-frequency effects created by Reverb
    /// properties are measured. Unit: Hz Range [1000.0f .. 20000.0f] Default: 5000.0f
    /// </summary>
    EaxReverbHFReference = 0x0014,
    /// <summary>
    /// The property LF reference determines the frequency at which the low-frequency effects created by Reverb
    /// properties are measured. Unit: Hz Range [20.0f .. 1000.0f] Default: 250.0f
    /// </summary>
    EaxReverbLFReference = 0x0015,
    /// <summary>
    /// The Room Rolloff Factor property is one of two methods available to attenuate the reflected sound (containing
    /// both reflections and reverberation) according to source-listener distance. It's defined the same way as OpenAL
    /// Rolloff Factor, but operates on reverb sound instead of direct-path sound. Range [0.0f .. 10.0f] Default: 0.0f
    /// </summary>
    EaxReverbRoomRolloffFactor = 0x0016,
}

/// <summary>
/// A list of valid Int32 Effect/GetEffect parameters.
/// </summary>
public enum EffectInteger
{
    /// <summary>
    /// This property sets the waveform shape of the low-frequency oscillator that controls the delay time of the
    /// delayed signals. Unit: (0) Sinusoid, (1) Triangle Range [0 .. 1] Default: 1
    /// </summary>
    ChorusWaveform = 0x0001,
    /// <summary>
    /// This property controls the phase difference between the left and right low-frequency oscillators. At zero
    /// degrees the two low-frequency oscillators are synchronized. Unit: Degrees Range [-180 .. 180] Default: 90
    /// </summary>
    ChorusPhase = 0x0002,
    /// <summary>
    /// Selects the shape of the low-frequency oscillator waveform that controls the amount of the delay of the
    /// sampled signal. Unit: (0) Sinusoid, (1) Triangle Range [0 .. 1] Default: 1
    /// </summary>
    FlangerWaveform = 0x0001,
    /// <summary>
    /// This changes the phase difference between the left and right low-frequency oscillator's. At zero degrees the
    /// two low-frequency oscillators are synchronized. Range [-180 .. +180] Default: 0
    /// </summary>
    FlangerPhase = 0x0002,
    /// <summary>
    /// These select which internal signals are added together to produce the output. Unit: (0) Down, (1) Up, (2) Off
    /// Range [0 .. 2] Default: 0
    /// </summary>
    FrequencyShifterLeftDirection = 0x0002,
    /// <summary>
    /// These select which internal signals are added together to produce the output. Unit: (0) Down, (1) Up, (2) Off
    /// Range [0 .. 2] Default: 0
    /// </summary>
    FrequencyShifterRightDirection = 0x0003,
    /// <summary>
    /// Sets the vocal morpher 4-band formant filter A, used to impose vocal tract effects upon the input signal. The
    /// vocal morpher is not necessarily intended for use on voice signals; it is primarily intended for pitched noise
    /// effects, vocal-like wind effects, etc. Unit: Use enum EfxFormantFilterSettings Range [0 .. 29] Default: 0, "Phoneme
    /// A"
    /// </summary>
    VocalMorpherPhonemeA = 0x0001,
    /// <summary>
    /// This is used to adjust the pitch of phoneme filter A in 1-semitone increments. Unit: Semitones Range [-24 ..
    /// +24] Default: 0
    /// </summary>
    VocalMorpherPhonemeACoarseTuning = 0x0002,
    /// <summary>
    /// Sets the vocal morpher 4-band formant filter B, used to impose vocal tract effects upon the input signal. The
    /// vocal morpher is not necessarily intended for use on voice signals; it is primarily intended for pitched noise
    /// effects, vocal-like wind effects, etc. Unit: Use enum EfxFormantFilterSettings Range [0 .. 29] Default: 10,
    /// "Phoneme ER"
    /// </summary>
    VocalMorpherPhonemeB = 0x0003,
    /// <summary>
    /// This is used to adjust the pitch of phoneme filter B in 1-semitone increments. Unit: Semitones Range [-24 ..
    /// +24] Default: 0
    /// </summary>
    VocalMorpherPhonemeBCoarseTuning = 0x0004,
    /// <summary>
    /// This controls the shape of the low-frequency oscillator used to morph between the two phoneme filters. Unit:
    /// (0) Sinusoid, (1) Triangle, (2) Sawtooth Range [0 .. 2] Default: 0
    /// </summary>
    VocalMorpherWaveform = 0x0005,
    /// <summary>
    /// This sets the number of semitones by which the pitch is shifted. There are 12 semitones per octave. Unit:
    /// Semitones Range [-12 .. +12] Default: +12
    /// </summary>
    PitchShifterCoarseTune = 0x0001,
    /// <summary>
    /// This sets the number of cents between Semitones a pitch is shifted. A Cent is 1/100th of a Semitone. Unit:
    /// Cents Range [-50 .. +50] Default: 0
    /// </summary>
    PitchShifterFineTune = 0x0002,
    /// <summary>
    /// This controls which waveform is used as the carrier signal. Traditional ring modulator and tremolo effects
    /// generally use a sinusoidal carrier. Unit: (0) Sinusoid, (1) Sawtooth, (2) Square Range [0 .. 2] Default: 0
    /// </summary>
    RingModulatorWaveform = 0x0003,
    /// <summary>
    /// Enabling this will result in audio exhibiting smaller variation in intensity between the loudest and quietest
    /// portions. Unit: (0) Off, (1) On Range [0 .. 1] Default: 1
    /// </summary>
    CompressorOnoff = 0x0001,
    /// <summary>
    /// When this flag is set, the high-frequency decay time automatically stays below a limit value that's derived
    /// from the setting of the property Air Absorption HF. Unit: (0) False, (1) True Range [False, True] Default: True
    /// </summary>
    ReverbDecayHFLimit = 0x000D,
    /// <summary>
    /// When this flag is set, the high-frequency decay time automatically stays below a limit value that's derived
    /// from the setting of the property AirAbsorptionGainHF. Unit: (0) False, (1) True Range [False, True] Default: True
    /// </summary>
    EaxReverbDecayHFLimit = 0x0017,
    /// <summary>
    /// Used with the enum EfxEffectType as it's parameter.
    /// </summary>
    EffectType = 0x8001,
}

/// <summary>
/// A list of valid <see cref="float"/> AuxiliaryEffectSlot/GetAuxiliaryEffectSlot parameters.
/// </summary>
public enum EffectSlotFloat
{
    /// <summary>
    /// Range [0.0f .. 1.0f]
    /// Default: 1.0f
    ///
    /// This property is used to specify an output level for the Auxiliary Effect Slot. Setting the gain to 0.0f mutes
    /// the output.
    /// </summary>
    Gain = 0x0002,
}

/// <summary>
/// A list of valid <see cref="int"/> AuxiliaryEffectSlot/GetAuxiliaryEffectSlot parameters.
/// </summary>
public enum EffectSlotInteger
{
    /// <summary>
    /// This property is used to attach an Effect object to the Auxiliary Effect Slot object. After the attachment,
    /// the Auxiliary Effect Slot object will contain the effect type and have the same effect parameters that were
    /// stored in the Effect object. Any Sources feeding the Auxiliary Effect Slot will immediate feed the new
    /// effect type and new effect parameters.
    /// </summary>
    Effect = 0x0001,
    /// <summary>
    /// This property is used to enable or disable automatic send adjustments based on the physical positions of the
    /// sources and the listener. This property should be enabled when an application wishes to use a reverb effect
    /// to simulate the environment surrounding a listener or a collection of Sources.
    /// </summary>
    AuxiliarySendAuto = 0x0003,
}

/// <summary>
/// Effect type definitions to be used with EfxEffecti.EffectType.
/// </summary>
public enum EffectType
{
    /// <summary>
    /// No Effect, disable. This Effect type is used when an Effect object is initially created.
    /// </summary>
    Null = 0x0000,
    /// <summary>
    /// The Reverb effect is the standard Effects Extension's environmental reverberation effect. It is available on
    /// all Generic Software and Generic Hardware devices.
    /// </summary>
    Reverb = 0x0001,
    /// <summary>
    /// The Chorus effect essentially replays the input audio accompanied by another slightly delayed version of the
    /// signal, creating a "doubling" effect.
    /// </summary>
    Chorus = 0x0002,
    /// <summary>
    /// The Distortion effect simulates turning up (overdriving) the gain stage on a guitar amplifier or adding a
    /// distortion pedal to an instrument's output.
    /// </summary>
    Distortion = 0x0003,
    /// <summary>
    /// The Echo effect generates discrete, delayed instances of the input signal.
    /// </summary>
    Echo = 0x0004,
    /// <summary>
    /// The Flanger effect creates a "tearing" or "whooshing" sound, like a jet flying overhead.
    /// </summary>
    Flanger = 0x0005,
    /// <summary>
    /// The Frequency shifter is a single-sideband modulator, which translates all the component frequencies of the
    /// input signal by an equal amount.
    /// </summary>
    FrequencyShifter = 0x0006,
    /// <summary>
    /// The Vocal morpher consists of a pair of 4-band formant filters, used to impose vocal tract effects upon the
    /// input signal.
    /// </summary>
    VocalMorpher = 0x0007,
    /// <summary>
    /// The Pitch shifter applies time-invariant pitch shifting to the input signal, over a one octave range and
    /// controllable at a semi-tone and cent resolution.
    /// </summary>
    PitchShifter = 0x0008,
    /// <summary>
    /// The Ring modulator multiplies an input signal by a carrier signal in the time domain, resulting in tremolo or
    /// inharmonic effects.
    /// </summary>
    RingModulator = 0x0009,
    /// <summary>
    /// The Auto-wah effect emulates the sound of a wah-wah pedal used with an electric guitar, or a mute on a brass
    /// instrument.
    /// </summary>
    Autowah = 0x000A,
    /// <summary>
    /// The Compressor will boost quieter portions of the audio, while louder portions will stay the same or may even
    /// be reduced.
    /// </summary>
    Compressor = 0x000B,
    /// <summary>
    /// The Equalizer is very flexible, providing tonal control over four different adjustable frequency ranges.
    /// </summary>
    Equalizer = 0x000C,
    /// <summary>
    /// The EAX Reverb has a more advanced parameter set than EfxEffectType.Reverb, but is only natively supported on
    /// devices that support the EAX 3.0 or above.
    /// </summary>
    EaxReverb = 0x8000,
}

/// <summary>
/// A list of valid Math.Vector3 Effect/GetEffect parameters.
/// </summary>
public enum EffectVector3
{
    /// <summary>
    /// Reverb Pan does for the Reverb what Reflections Pan does for the Reflections. Unit: Vector3 of length 0f to 1f
    /// Default: {0.0f, 0.0f, 0.0f}
    /// </summary>
    EaxReverbLateReverbPan = 0x000E,
    /// <summary>
    /// This Vector3 controls the spatial distribution of the cluster of early reflections. The direction of this
    /// vector controls the global direction of the reflections, while its magnitude controls how focused the reflections
    /// are towards this direction. For legacy reasons this Vector3 follows a left-handed co-ordinate system! Note that
    /// OpenAL uses a right-handed coordinate system. Unit: Vector3 of length 0f to 1f Default: {0.0f, 0.0f, 0.0f}
    /// </summary>
    EaxReverbReflectionsPan = 0x000B,
}

/// <summary>
/// Defines new context attributes.
/// </summary>
public enum EFXContextAttributes
{
    /// <summary>
    /// Default: 2
    ///
    /// This Context property can be passed to OpenAL during Context creation (alcCreateContext) to
    /// request a maximum number of Auxiliary Sends desired on each Source. It is not guaranteed that the desired
    /// number of sends will be available, so an application should query this property after creating the context
    /// using alcGetIntergerv.
    /// </summary>
    MaxAuxiliarySends = 0x20003,
}

/// <summary>
/// Defines new integer properties on the OpenAL context.
/// </summary>
public enum EFXContextInteger
{
    /// <summary>
    /// This property can be used by the application to retrieve the Major version number of the
    /// Effects Extension supported by this OpenAL implementation. As this is a Context property is should be
    /// retrieved using alcGetIntegerv.
    /// </summary>
    EFXMajorVersion = 0x20001,
    /// <summary>
    /// This property can be used by the application to retrieve the Minor version number of the
    /// Effects Extension supported by this OpenAL implementation. As this is a Context property is should be
    /// retrieved using alcGetIntegerv.
    /// </summary>
    EFXMinorVersion = 0x20002,
    /// <summary>
    /// Default: 2
    ///
    /// This Context property can be passed to OpenAL during Context creation (alcCreateContext) to
    /// request a maximum number of Auxiliary Sends desired on each Source. It is not guaranteed that the desired
    /// number of sends will be available, so an application should query this property after creating the context
    /// using alcGetIntergerv.
    /// </summary>
    MaxAuxiliarySends = 0x20003,
}

/// <summary>
/// A list of valid <see cref="float"/> Listener/GetListener parameters.
/// </summary>
public enum EFXListenerFloat
{
    /// <summary>
    /// centimeters 0.01f
    /// meters 1.0f
    /// kilometers 1000.0f
    /// Range [float.MinValue .. float.MaxValue]
    /// Default: 1.0f.
    ///
    /// This setting is critical if Air Absorption effects are enabled because the amount of Air
    /// Absorption applied is directly related to the real-world distance between the Source and the Listener.
    /// </summary>
    EfxMetersPerUnit = 0x20004,
}

/// <summary>
/// A list of valid <see cref="bool"/> Source/GetSource parameters.
/// </summary>
public enum EFXSourceBoolean
{
    /// <summary>
    /// Default: True
    ///
    /// If this Source property is set to True, this Source’s direct-path is automatically filtered
    /// according to the orientation of the source relative to the listener and the setting of the Source property
    /// Sourcef.ConeOuterGainHF.
    /// </summary>
    DirectFilterGainHighFrequencyAuto = 0x2000A,
    /// <summary>
    /// Default: True
    ///
    /// If this Source property is set to True, the intensity of this Source’s reflected sound is
    /// automatically attenuated according to source-listener distance and source directivity (as determined by the cone
    /// parameters). If it is False, the reflected sound is not attenuated according to distance and directivity.
    /// </summary>
    AuxiliarySendFilterGainAuto = 0x2000B,
    /// <summary>
    /// Default: True
    ///
    /// If this Source property is AL_TRUE (its default value), the intensity of this Source’s
    /// reflected sound at high frequencies will be automatically attenuated according to the high-frequency source
    /// directivity as set by the Sourcef.ConeOuterGainHF property. If this property is AL_FALSE, the Source’s reflected
    /// sound is not filtered at all according to the Source’s directivity.
    /// </summary>
    AuxiliarySendFilterGainHighFrequencyAuto = 0x2000C,
}

/// <summary>
/// A list of valid <see cref="float"/> Source/GetSource parameters.
/// </summary>
public enum EFXSourceFloat
{
    /// <summary>
    /// Range [0.0f .. 10.0f]
    /// Default: 0.0f
    ///
    /// This property is a multiplier on the amount of Air Absorption applied to the Source. The
    /// AL_AIR_ABSORPTION_FACTOR is multiplied by an internal Air Absorption Gain HF value of 0.994 (-0.05dB) per meter
    /// which represents normal atmospheric humidity and temperature.
    /// </summary>
    AirAbsorptionFactor = 0x20007,
    /// <summary>
    /// Range [0.0f .. 10.0f]
    /// Default: 0.0f
    ///
    /// This property is defined the same way as the Reverb Room Rolloff property: it is one of two
    /// methods available in the Effect Extension to attenuate the reflected sound (early reflections and reverberation)
    /// according to source-listener distance.
    /// </summary>
    RoomRolloffFactor = 0x20008,
    /// <summary>
    /// Range [0.0f .. 1.0f]
    /// Default: 1.0f
    ///
    /// A directed Source points in a specified direction. The Source sounds at full volume when the
    /// listener is directly in front of the source; it is attenuated as the listener circles the Source away from the
    /// front.
    /// </summary>
    ConeOuterGainHighFrequency = 0x20009,
}

/// <summary>
/// A list of valid <see cref="int"/> Source/GetSource parameters.
/// </summary>
public enum EFXSourceInteger
{
    /// <summary>
    /// This Source property is used to apply filtering on the direct-path (dry signal) of a Source.
    /// </summary>
    DirectFilter = 0x20005,
}

/// <summary>
/// A list of valid <see cref="int"/> Source/GetSource parameters.
/// </summary>
public enum EFXSourceInteger3
{
    /// <summary>
    /// This Source property is used to establish connections between Sources and Auxiliary Effect
    /// Slots. For a Source to feed an Effect that has been loaded into an Auxiliary Effect Slot the application must
    /// configure one of the Source’s auxiliary sends. This process involves setting 3 variables – the destination
    /// Auxiliary Effect Slot ID, the Auxiliary Send number, and an optional Filter ID.
    /// </summary>
    AuxiliarySendFilter = 0x20006,
}

/// <summary>
/// A list of valid <see cref="float"/> Filter/GetFilter parameters.
/// </summary>
public enum FilterFloat
{
    /// <summary>
    /// Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    LowpassGain = 0x0001,
    /// <summary>
    /// Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    LowpassGainHF = 0x0002,
    /// <summary>
    /// Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    HighpassGain = 0x0001,
    /// <summary>
    /// Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    HighpassGainLF = 0x0002,
    /// <summary>
    /// Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    BandpassGain = 0x0001,
    /// <summary>
    /// Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    BandpassGainLF = 0x0002,
    /// <summary>
    /// Range [0.0f .. 1.0f] Default: 1.0f
    /// </summary>
    BandpassGainHF = 0x0003,
}

/// <summary>
/// A list of valid <see cref="int"/> Filter/GetFilter parameters.
/// </summary>
public enum FilterInteger
{
    /// <summary>
    /// Used with the enum EfxFilterType as Parameter to select a filter logic.
    /// </summary>
    FilterType = 0x8001,
}

/// <summary>
/// Filter type definitions to be used with EfxFilteri.FilterType.
/// </summary>
public enum FilterType
{
    /// <summary>
    /// No Filter, disable. This Filter type is used when a Filter object is initially created.
    /// </summary>
    Null = 0x0000,
    /// <summary>
    /// A low-pass filter is used to remove high frequency content from a signal.
    /// </summary>
    Lowpass = 0x0001,
    /// <summary>
    /// Currently not implemented. A high-pass filter is used to remove low frequency content from a signal.
    /// </summary>
    Highpass = 0x0002,
    /// <summary>
    /// Currently not implemented. A band-pass filter is used to remove high and low frequency content from a signal.
    /// </summary>
    Bandpass = 0x0003,
}

/// <summary>
/// Vocal morpher effect parameters. If both parameters are set to the same phoneme, that determines the filtering
/// effect that will be heard. If these two parameters are set to different phonemes, the filtering effect will morph
/// between the two settings at a rate specified by EfxEffectf.VocalMorpherRate.
/// </summary>
public enum FormantFilterSettings
{
    /// <summary>
    /// The A phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeA = 0,
    /// <summary>
    /// The E phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeE = 1,
    /// <summary>
    /// The I phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeI = 2,
    /// <summary>
    /// The O phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeO = 3,
    /// <summary>
    /// The U phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeU = 4,
    /// <summary>
    /// The AA phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeAA = 5,
    /// <summary>
    /// The AE phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeAE = 6,
    /// <summary>
    /// The AH phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeAH = 7,
    /// <summary>
    /// The AO phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeAO = 8,
    /// <summary>
    /// The EH phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeEH = 9,
    /// <summary>
    /// The ER phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeER = 10,
    /// <summary>
    /// The IH phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeIH = 11,
    /// <summary>
    /// The IY phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeIY = 12,
    /// <summary>
    /// The UH phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeUH = 13,
    /// <summary>
    /// The UW phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeUW = 14,
    /// <summary>
    /// The B phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeB = 15,
    /// <summary>
    /// The D phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeD = 16,
    /// <summary>
    /// The F phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeF = 17,
    /// <summary>
    /// The G phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeG = 18,
    /// <summary>
    /// The J phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeJ = 19,
    /// <summary>
    /// The K phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeK = 20,
    /// <summary>
    /// The L phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeL = 21,
    /// <summary>
    /// The M phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeM = 22,
    /// <summary>
    /// The N phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeN = 23,
    /// <summary>
    /// The P phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeP = 24,
    /// <summary>
    /// The R phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeR = 25,
    /// <summary>
    /// The S phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeS = 26,
    /// <summary>
    /// The T phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeT = 27,
    /// <summary>
    /// The V phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeV = 28,
    /// <summary>
    /// The Z phoneme of the vocal morpher.
    /// </summary>
    VocalMorpherPhonemeZ = 29,
}

/// <summary>
/// May be passed at context construction time to indicate the number of desired auxiliary effect slot sends per
/// source.
/// </summary>
public enum MaxAuxiliarySends
{
    /// <summary>
    /// Will chose a reliably working parameter.
    /// </summary>
    UseDriverDefault = 0,
    /// <summary>
    /// One send per source.
    /// </summary>
    One = 1,
    /// <summary>
    /// Two sends per source.
    /// </summary>
    Two = 2,
    /// <summary>
    /// Three sends per source.
    /// </summary>
    Three = 3,
    /// <summary>
    /// Four sends per source.
    /// </summary>
    Four = 4,
}

#endregion

#region Presets

/// <summary>
/// A set of reverb presets that can be used with the extension.
/// </summary>
public static class ReverbPresets
{
    /// <summary>
    /// A reverb preset (approximating a generic location).
    /// </summary>
    public static readonly ReverbProperties Generic = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.8913f,
        1.0000f,
        1.4900f,
        0.8300f,
        1.0000f,
        0.0500f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a padded cell).
    /// </summary>
    public static readonly ReverbProperties PaddedCell = new ReverbProperties
    (
        0.1715f,
        1.0000f,
        0.3162f,
        0.0010f,
        1.0000f,
        0.1700f,
        0.1000f,
        1.0000f,
        0.2500f,
        0.0010f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2691f,
        0.0020f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a room).
    /// </summary>
    public static readonly ReverbProperties Room = new ReverbProperties
    (
        0.4287f,
        1.0000f,
        0.3162f,
        0.5929f,
        1.0000f,
        0.4000f,
        0.8300f,
        1.0000f,
        0.1503f,
        0.0020f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.0629f,
        0.0030f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a bathroom).
    /// </summary>
    public static readonly ReverbProperties Bathroom = new ReverbProperties
    (
        0.1715f,
        1.0000f,
        0.3162f,
        0.2512f,
        1.0000f,
        1.4900f,
        0.5400f,
        1.0000f,
        0.6531f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        3.2734f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a living room).
    /// </summary>
    public static readonly ReverbProperties LivingRoom = new ReverbProperties
    (
        0.9766f,
        1.0000f,
        0.3162f,
        0.0010f,
        1.0000f,
        0.5000f,
        0.1000f,
        1.0000f,
        0.2051f,
        0.0030f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2805f,
        0.0040f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a stone room).
    /// </summary>
    public static readonly ReverbProperties StoneRoom = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.7079f,
        1.0000f,
        2.3100f,
        0.6400f,
        1.0000f,
        0.4411f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1003f,
        0.0170f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an auditorium).
    /// </summary>
    public static readonly ReverbProperties Auditorium = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.5781f,
        1.0000f,
        4.3200f,
        0.5900f,
        1.0000f,
        0.4032f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7170f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a concert hall).
    /// </summary>
    public static readonly ReverbProperties ConcertHall = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.5623f,
        1.0000f,
        3.9200f,
        0.7000f,
        1.0000f,
        0.2427f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.9977f,
        0.0290f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a cave).
    /// </summary>
    public static readonly ReverbProperties Cave = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        1.0000f,
        1.0000f,
        2.9100f,
        1.3000f,
        1.0000f,
        0.5000f,
        0.0150f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7063f,
        0.0220f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating an arena).
    /// </summary>
    public static readonly ReverbProperties Arena = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.4477f,
        1.0000f,
        7.2400f,
        0.3300f,
        1.0000f,
        0.2612f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.0186f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a hangar).
    /// </summary>
    public static readonly ReverbProperties Hangar = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.3162f,
        1.0000f,
        10.0500f,
        0.2300f,
        1.0000f,
        0.5000f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2560f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a carpeted hallway).
    /// </summary>
    public static readonly ReverbProperties CarpetedHallway = new ReverbProperties
    (
        0.4287f,
        1.0000f,
        0.3162f,
        0.0100f,
        1.0000f,
        0.3000f,
        0.1000f,
        1.0000f,
        0.1215f,
        0.0020f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1531f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a hallway).
    /// </summary>
    public static readonly ReverbProperties Hallway = new ReverbProperties
    (
        0.3645f,
        1.0000f,
        0.3162f,
        0.7079f,
        1.0000f,
        1.4900f,
        0.5900f,
        1.0000f,
        0.2458f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.6615f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a stone corridor).
    /// </summary>
    public static readonly ReverbProperties StoneCorridor = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.7612f,
        1.0000f,
        2.7000f,
        0.7900f,
        1.0000f,
        0.2472f,
        0.0130f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.5758f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an alley).
    /// </summary>
    public static readonly ReverbProperties Alley = new ReverbProperties
    (
        1.0000f,
        0.3000f,
        0.3162f,
        0.7328f,
        1.0000f,
        1.4900f,
        0.8600f,
        1.0000f,
        0.2500f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.9954f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1250f,
        0.9500f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a forest).
    /// </summary>
    public static readonly ReverbProperties Forest = new ReverbProperties
    (
        1.0000f,
        0.3000f,
        0.3162f,
        0.0224f,
        1.0000f,
        1.4900f,
        0.5400f,
        1.0000f,
        0.0525f,
        0.1620f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7682f,
        0.0880f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1250f,
        1.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a city).
    /// </summary>
    public static readonly ReverbProperties City = new ReverbProperties
    (
        1.0000f,
        0.5000f,
        0.3162f,
        0.3981f,
        1.0000f,
        1.4900f,
        0.6700f,
        1.0000f,
        0.0730f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1427f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a mountain).
    /// </summary>
    public static readonly ReverbProperties Mountains = new ReverbProperties
    (
        1.0000f,
        0.2700f,
        0.3162f,
        0.0562f,
        1.0000f,
        1.4900f,
        0.2100f,
        1.0000f,
        0.0407f,
        0.3000f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1919f,
        0.1000f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        1.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a quarry).
    /// </summary>
    public static readonly ReverbProperties Quarry = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.3162f,
        1.0000f,
        1.4900f,
        0.8300f,
        1.0000f,
        0.0000f,
        0.0610f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.7783f,
        0.0250f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1250f,
        0.7000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a plain).
    /// </summary>
    public static readonly ReverbProperties Plain = new ReverbProperties
    (
        1.0000f,
        0.2100f,
        0.3162f,
        0.1000f,
        1.0000f,
        1.4900f,
        0.5000f,
        1.0000f,
        0.0585f,
        0.1790f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1089f,
        0.1000f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        1.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a parking lot).
    /// </summary>
    public static readonly ReverbProperties ParkingLot = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        1.0000f,
        1.0000f,
        1.6500f,
        1.5000f,
        1.0000f,
        0.2082f,
        0.0080f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2652f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a sewer pipe).
    /// </summary>
    public static readonly ReverbProperties Sewerpipe = new ReverbProperties
    (
        0.3071f,
        0.8000f,
        0.3162f,
        0.3162f,
        1.0000f,
        2.8100f,
        0.1400f,
        1.0000f,
        1.6387f,
        0.0140f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        3.2471f,
        0.0210f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an underwater location).
    /// </summary>
    public static readonly ReverbProperties Underwater = new ReverbProperties
    (
        0.3645f,
        1.0000f,
        0.3162f,
        0.0100f,
        1.0000f,
        1.4900f,
        0.1000f,
        1.0000f,
        0.5963f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        7.0795f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        1.1800f,
        0.3480f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a drugged state).
    /// </summary>
    public static readonly ReverbProperties Drugged = new ReverbProperties
    (
        0.4287f,
        0.5000f,
        0.3162f,
        1.0000f,
        1.0000f,
        8.3900f,
        1.3900f,
        1.0000f,
        0.8760f,
        0.0020f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        3.1081f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        1.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a dizzy state).
    /// </summary>
    public static readonly ReverbProperties Dizzy = new ReverbProperties
    (
        0.3645f,
        0.6000f,
        0.3162f,
        0.6310f,
        1.0000f,
        17.2300f,
        0.5600f,
        1.0000f,
        0.1392f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.4937f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        1.0000f,
        0.8100f,
        0.3100f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a psychotic state).
    /// </summary>
    public static readonly ReverbProperties Psychotic = new ReverbProperties
    (
        0.0625f,
        0.5000f,
        0.3162f,
        0.8404f,
        1.0000f,
        7.5600f,
        0.9100f,
        1.0000f,
        0.4864f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        2.4378f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        4.0000f,
        1.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /* Castle Presets */

    /// <summary>
    /// A reverb preset (approximating a small room in a castle).
    /// </summary>
    public static readonly ReverbProperties CastleSmallRoom = new ReverbProperties
    (
        1.0000f,
        0.8900f,
        0.3162f,
        0.3981f,
        0.1000f,
        1.2200f,
        0.8300f,
        0.3100f,
        0.8913f,
        0.0220f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.9953f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1380f,
        0.0800f,
        0.2500f,
        0.0000f,
        0.9943f,
        5168.6001f,
        139.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a short passage in a castle).
    /// </summary>
    public static readonly ReverbProperties CastleShortPassage = new ReverbProperties
    (
        1.0000f,
        0.8900f,
        0.3162f,
        0.3162f,
        0.1000f,
        2.3200f,
        0.8300f,
        0.3100f,
        0.8913f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0230f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1380f,
        0.0800f,
        0.2500f,
        0.0000f,
        0.9943f,
        5168.6001f,
        139.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a medium room in a castle).
    /// </summary>
    public static readonly ReverbProperties CastleMediumRoom = new ReverbProperties
    (
        1.0000f,
        0.9300f,
        0.3162f,
        0.2818f,
        0.1000f,
        2.0400f,
        0.8300f,
        0.4600f,
        0.6310f,
        0.0220f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.5849f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1550f,
        0.0300f,
        0.2500f,
        0.0000f,
        0.9943f,
        5168.6001f,
        139.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a large room in a castle).
    /// </summary>
    public static readonly ReverbProperties CastleLargeRoom = new ReverbProperties
    (
        1.0000f,
        0.8200f,
        0.3162f,
        0.2818f,
        0.1259f,
        2.5300f,
        0.8300f,
        0.5000f,
        0.4467f,
        0.0340f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0160f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1850f,
        0.0700f,
        0.2500f,
        0.0000f,
        0.9943f,
        5168.6001f,
        139.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a long passage in a castle).
    /// </summary>
    public static readonly ReverbProperties CastleLongPassage = new ReverbProperties
    (
        1.0000f,
        0.8900f,
        0.3162f,
        0.3981f,
        0.1000f,
        3.4200f,
        0.8300f,
        0.3100f,
        0.8913f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.4125f,
        0.0230f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1380f,
        0.0800f,
        0.2500f,
        0.0000f,
        0.9943f,
        5168.6001f,
        139.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a hall in a castle).
    /// </summary>
    public static readonly ReverbProperties CastleHall = new ReverbProperties
    (
        1.0000f,
        0.8100f,
        0.3162f,
        0.2818f,
        0.1778f,
        3.1400f,
        0.7900f,
        0.6200f,
        0.1778f,
        0.0560f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1220f,
        0.0240f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5168.6001f,
        139.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a cupboard in a castle).
    /// </summary>
    public static readonly ReverbProperties CastleCupboard = new ReverbProperties
    (
        1.0000f,
        0.8900f,
        0.3162f,
        0.2818f,
        0.1000f,
        0.6700f,
        0.8700f,
        0.3100f,
        1.4125f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        3.5481f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1380f,
        0.0800f,
        0.2500f,
        0.0000f,
        0.9943f,
        5168.6001f,
        139.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a castle courtyard).
    /// </summary>
    public static readonly ReverbProperties CastleCourtyard = new ReverbProperties
    (
        1.0000f,
        0.4200f,
        0.3162f,
        0.4467f,
        0.1995f,
        2.1300f,
        0.6100f,
        0.2300f,
        0.2239f,
        0.1600f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7079f,
        0.0360f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.3700f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating an alcove in a castle).
    /// </summary>
    public static readonly ReverbProperties CastleAlcove = new ReverbProperties
    (
        1.0000f,
        0.8900f,
        0.3162f,
        0.5012f,
        0.1000f,
        1.6400f,
        0.8700f,
        0.3100f,
        1.0000f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.4125f,
        0.0340f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1380f,
        0.0800f,
        0.2500f,
        0.0000f,
        0.9943f,
        5168.6001f,
        139.5000f,
        0.0000f,
        0x1
    );

    /* Factory Presets */

    /// <summary>
    /// A reverb preset (approximating a small room in a factory).
    /// </summary>
    public static readonly ReverbProperties FactorySmallRoom = new ReverbProperties
    (
        0.3645f,
        0.8200f,
        0.3162f,
        0.7943f,
        0.5012f,
        1.7200f,
        0.6500f,
        1.3100f,
        0.7079f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.7783f,
        0.0240f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1190f,
        0.0700f,
        0.2500f,
        0.0000f,
        0.9943f,
        3762.6001f,
        362.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a short passage in a factory).
    /// </summary>
    public static readonly ReverbProperties FactoryShortPassage = new ReverbProperties
    (
        0.3645f,
        0.6400f,
        0.2512f,
        0.7943f,
        0.5012f,
        2.5300f,
        0.6500f,
        1.3100f,
        1.0000f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0380f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1350f,
        0.2300f,
        0.2500f,
        0.0000f,
        0.9943f,
        3762.6001f,
        362.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a medium room in a factory).
    /// </summary>
    public static readonly ReverbProperties FactoryMediumRoom = new ReverbProperties
    (
        0.4287f,
        0.8200f,
        0.2512f,
        0.7943f,
        0.5012f,
        2.7600f,
        0.6500f,
        1.3100f,
        0.2818f,
        0.0220f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.4125f,
        0.0230f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1740f,
        0.0700f,
        0.2500f,
        0.0000f,
        0.9943f,
        3762.6001f,
        362.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a large room in a factory).
    /// </summary>
    public static readonly ReverbProperties FactoryLargeRoom = new ReverbProperties
    (
        0.4287f,
        0.7500f,
        0.2512f,
        0.7079f,
        0.6310f,
        4.2400f,
        0.5100f,
        1.3100f,
        0.1778f,
        0.0390f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1220f,
        0.0230f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2310f,
        0.0700f,
        0.2500f,
        0.0000f,
        0.9943f,
        3762.6001f,
        362.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a long passage in a factory).
    /// </summary>
    public static readonly ReverbProperties FactoryLongPassage = new ReverbProperties
    (
        0.3645f,
        0.6400f,
        0.2512f,
        0.7943f,
        0.5012f,
        4.0600f,
        0.6500f,
        1.3100f,
        1.0000f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0370f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1350f,
        0.2300f,
        0.2500f,
        0.0000f,
        0.9943f,
        3762.6001f,
        362.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a hall in a factory).
    /// </summary>
    public static readonly ReverbProperties FactoryHall = new ReverbProperties
    (
        0.4287f,
        0.7500f,
        0.3162f,
        0.7079f,
        0.6310f,
        7.4300f,
        0.5100f,
        1.3100f,
        0.0631f,
        0.0730f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.8913f,
        0.0270f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0700f,
        0.2500f,
        0.0000f,
        0.9943f,
        3762.6001f,
        362.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a cupboard in a factory).
    /// </summary>
    public static readonly ReverbProperties FactoryCupboard = new ReverbProperties
    (
        0.3071f,
        0.6300f,
        0.2512f,
        0.7943f,
        0.5012f,
        0.4900f,
        0.6500f,
        1.3100f,
        1.2589f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.9953f,
        0.0320f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1070f,
        0.0700f,
        0.2500f,
        0.0000f,
        0.9943f,
        3762.6001f,
        362.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a factory courtyard).
    /// </summary>
    public static readonly ReverbProperties FactoryCourtyard = new ReverbProperties
    (
        0.3071f,
        0.5700f,
        0.3162f,
        0.3162f,
        0.6310f,
        2.3200f,
        0.2900f,
        0.5600f,
        0.2239f,
        0.1400f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.3981f,
        0.0390f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.2900f,
        0.2500f,
        0.0000f,
        0.9943f,
        3762.6001f,
        362.5000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an alcove in a factory).
    /// </summary>
    public static readonly ReverbProperties FactoryAlcove = new ReverbProperties
    (
        0.3645f,
        0.5900f,
        0.2512f,
        0.7943f,
        0.5012f,
        3.1400f,
        0.6500f,
        1.3100f,
        1.4125f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.0000f,
        0.0380f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1140f,
        0.1000f,
        0.2500f,
        0.0000f,
        0.9943f,
        3762.6001f,
        362.5000f,
        0.0000f,
        0x1
    );

    /* Ice Palace Presets */

    /// <summary>
    /// A reverb preset (approximating a small room in an ice palace).
    /// </summary>
    public static readonly ReverbProperties IcePalaceSmallRoom = new ReverbProperties
    (
        1.0000f,
        0.8400f,
        0.3162f,
        0.5623f,
        0.2818f,
        1.5100f,
        1.5300f,
        0.2700f,
        0.8913f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.4125f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1640f,
        0.1400f,
        0.2500f,
        0.0000f,
        0.9943f,
        12428.5000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a short passage in an ice palace).
    /// </summary>
    public static readonly ReverbProperties IcePalaceShortPassage = new ReverbProperties
    (
        1.0000f,
        0.7500f,
        0.3162f,
        0.5623f,
        0.2818f,
        1.7900f,
        1.4600f,
        0.2800f,
        0.5012f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1220f,
        0.0190f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1770f,
        0.0900f,
        0.2500f,
        0.0000f,
        0.9943f,
        12428.5000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a medium room in an ice palace).
    /// </summary>
    public static readonly ReverbProperties IcePalaceMediumRoom = new ReverbProperties
    (
        1.0000f,
        0.8700f,
        0.3162f,
        0.5623f,
        0.4467f,
        2.2200f,
        1.5300f,
        0.3200f,
        0.3981f,
        0.0390f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1220f,
        0.0270f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1860f,
        0.1200f,
        0.2500f,
        0.0000f,
        0.9943f,
        12428.5000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a large room in an ice palace).
    /// </summary>
    public static readonly ReverbProperties IcePalaceLargeRoom = new ReverbProperties
    (
        1.0000f,
        0.8100f,
        0.3162f,
        0.5623f,
        0.4467f,
        3.1400f,
        1.5300f,
        0.3200f,
        0.2512f,
        0.0390f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.0000f,
        0.0270f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2140f,
        0.1100f,
        0.2500f,
        0.0000f,
        0.9943f,
        12428.5000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a long passage in an ice palace).
    /// </summary>
    public static readonly ReverbProperties IcePalaceLongPassage = new ReverbProperties
    (
        1.0000f,
        0.7700f,
        0.3162f,
        0.5623f,
        0.3981f,
        3.0100f,
        1.4600f,
        0.2800f,
        0.7943f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0250f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1860f,
        0.0400f,
        0.2500f,
        0.0000f,
        0.9943f,
        12428.5000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a hall in an ice palace).
    /// </summary>
    public static readonly ReverbProperties IcePalaceHall = new ReverbProperties
    (
        1.0000f,
        0.7600f,
        0.3162f,
        0.4467f,
        0.5623f,
        5.4900f,
        1.5300f,
        0.3800f,
        0.1122f,
        0.0540f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.6310f,
        0.0520f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2260f,
        0.1100f,
        0.2500f,
        0.0000f,
        0.9943f,
        12428.5000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a cupboard in an ice palace).
    /// </summary>
    public static readonly ReverbProperties IcePalaceCupboard = new ReverbProperties
    (
        1.0000f,
        0.8300f,
        0.3162f,
        0.5012f,
        0.2239f,
        0.7600f,
        1.5300f,
        0.2600f,
        1.1220f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.9953f,
        0.0160f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1430f,
        0.0800f,
        0.2500f,
        0.0000f,
        0.9943f,
        12428.5000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an ice palace courtyard).
    /// </summary>
    public static readonly ReverbProperties IcePalaceCourtyard = new ReverbProperties
    (
        1.0000f,
        0.5900f,
        0.3162f,
        0.2818f,
        0.3162f,
        2.0400f,
        1.2000f,
        0.3800f,
        0.3162f,
        0.1730f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.3162f,
        0.0430f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2350f,
        0.4800f,
        0.2500f,
        0.0000f,
        0.9943f,
        12428.5000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an alcove in an ice palace).
    /// </summary>
    public static readonly ReverbProperties IcePalaceAlcove = new ReverbProperties
    (
        1.0000f,
        0.8400f,
        0.3162f,
        0.5623f,
        0.2818f,
        2.7600f,
        1.4600f,
        0.2800f,
        1.1220f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.8913f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1610f,
        0.0900f,
        0.2500f,
        0.0000f,
        0.9943f,
        12428.5000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /* Space Station Presets */

    /// <summary>
    /// A reverb preset (approximating a small room in a space station).
    /// </summary>
    public static readonly ReverbProperties SpaceStationSmallRoom = new ReverbProperties
    (
        0.2109f,
        0.7000f,
        0.3162f,
        0.7079f,
        0.8913f,
        1.7200f,
        0.8200f,
        0.5500f,
        0.7943f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.4125f,
        0.0130f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1880f,
        0.2600f,
        0.2500f,
        0.0000f,
        0.9943f,
        3316.1001f,
        458.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a short passage in a space station).
    /// </summary>
    public static readonly ReverbProperties SpaceStationShortPassage = new ReverbProperties
    (
        0.2109f,
        0.8700f,
        0.3162f,
        0.6310f,
        0.8913f,
        3.5700f,
        0.5000f,
        0.5500f,
        1.0000f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1220f,
        0.0160f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1720f,
        0.2000f,
        0.2500f,
        0.0000f,
        0.9943f,
        3316.1001f,
        458.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a medium room in a space station).
    /// </summary>
    public static readonly ReverbProperties SpaceStationMediumRoom = new ReverbProperties
    (
        0.2109f,
        0.7500f,
        0.3162f,
        0.6310f,
        0.8913f,
        3.0100f,
        0.5000f,
        0.5500f,
        0.3981f,
        0.0340f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1220f,
        0.0350f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2090f,
        0.3100f,
        0.2500f,
        0.0000f,
        0.9943f,
        3316.1001f,
        458.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a large room in a space station).
    /// </summary>
    public static readonly ReverbProperties SpaceStationLargeRoom = new ReverbProperties
    (
        0.3645f,
        0.8100f,
        0.3162f,
        0.6310f,
        0.8913f,
        3.8900f,
        0.3800f,
        0.6100f,
        0.3162f,
        0.0560f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.8913f,
        0.0350f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2330f,
        0.2800f,
        0.2500f,
        0.0000f,
        0.9943f,
        3316.1001f,
        458.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a long passage in a space station).
    /// </summary>
    public static readonly ReverbProperties SpaceStationLongPassage = new ReverbProperties
    (
        0.4287f,
        0.8200f,
        0.3162f,
        0.6310f,
        0.8913f,
        4.6200f,
        0.6200f,
        0.5500f,
        1.0000f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0310f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.2300f,
        0.2500f,
        0.0000f,
        0.9943f,
        3316.1001f,
        458.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a hall in a space station).
    /// </summary>
    public static readonly ReverbProperties SpaceStationHall = new ReverbProperties
    (
        0.4287f,
        0.8700f,
        0.3162f,
        0.6310f,
        0.8913f,
        7.1100f,
        0.3800f,
        0.6100f,
        0.1778f,
        0.1000f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.6310f,
        0.0470f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.2500f,
        0.2500f,
        0.0000f,
        0.9943f,
        3316.1001f,
        458.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a cupboard in a space station).
    /// </summary>
    public static readonly ReverbProperties SpaceStationCupboard = new ReverbProperties
    (
        0.1715f,
        0.5600f,
        0.3162f,
        0.7079f,
        0.8913f,
        0.7900f,
        0.8100f,
        0.5500f,
        1.4125f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.7783f,
        0.0180f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1810f,
        0.3100f,
        0.2500f,
        0.0000f,
        0.9943f,
        3316.1001f,
        458.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an alcove in a space station).
    /// </summary>
    public static readonly ReverbProperties SpaceStationAlcove = new ReverbProperties
    (
        0.2109f,
        0.7800f,
        0.3162f,
        0.7079f,
        0.8913f,
        1.1600f,
        0.8100f,
        0.5500f,
        1.4125f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.0000f,
        0.0180f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1920f,
        0.2100f,
        0.2500f,
        0.0000f,
        0.9943f,
        3316.1001f,
        458.2000f,
        0.0000f,
        0x1
    );

    /* Wooden Galleon Presets */

    /// <summary>
    /// A reverb preset (approximating a small room in a wooden galleon).
    /// </summary>
    public static readonly ReverbProperties WoodenGalleonSmallRoom = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.1122f,
        0.3162f,
        0.7900f,
        0.3200f,
        0.8700f,
        1.0000f,
        0.0320f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.8913f,
        0.0290f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4705.0000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a short passage in a wooden galleon).
    /// </summary>
    public static readonly ReverbProperties WoodenGalleonShortPassage = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.1259f,
        0.3162f,
        1.7500f,
        0.5000f,
        0.8700f,
        0.8913f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.6310f,
        0.0240f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4705.0000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a medium room in a wooden galleon).
    /// </summary>
    public static readonly ReverbProperties WoodenGalleonMediumRoom = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.1000f,
        0.2818f,
        1.4700f,
        0.4200f,
        0.8200f,
        0.8913f,
        0.0490f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.8913f,
        0.0290f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4705.0000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a large room in a wooden galleon).
    /// </summary>
    public static readonly ReverbProperties WoodenGalleonLargeRoom = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.0891f,
        0.2818f,
        2.6500f,
        0.3300f,
        0.8200f,
        0.8913f,
        0.0660f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7943f,
        0.0490f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4705.0000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a long passsage in a wooden galleon).
    /// </summary>
    public static readonly ReverbProperties WoodenGalleonLongPassage = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.1000f,
        0.3162f,
        1.9900f,
        0.4000f,
        0.7900f,
        1.0000f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.4467f,
        0.0360f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4705.0000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a hall in a wooden galleon).
    /// </summary>
    public static readonly ReverbProperties WoodenGalleonHall = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.0794f,
        0.2818f,
        3.4500f,
        0.3000f,
        0.8200f,
        0.8913f,
        0.0880f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7943f,
        0.0630f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4705.0000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a cupboard in a wooden galleon).
    /// </summary>
    public static readonly ReverbProperties WoodenGalleonCupboard = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.1413f,
        0.3162f,
        0.5600f,
        0.4600f,
        0.9100f,
        1.1220f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1220f,
        0.0280f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4705.0000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a courtyard on a wooden galleon).
    /// </summary>
    public static readonly ReverbProperties WoodenGalleonCourtyard = new ReverbProperties
    (
        1.0000f,
        0.6500f,
        0.3162f,
        0.0794f,
        0.3162f,
        1.7900f,
        0.3500f,
        0.7900f,
        0.5623f,
        0.1230f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1000f,
        0.0320f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4705.0000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an alcove in a wooden galleon).
    /// </summary>
    public static readonly ReverbProperties WoodenGalleonAlcove = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.1259f,
        0.3162f,
        1.2200f,
        0.6200f,
        0.9100f,
        1.1220f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7079f,
        0.0240f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4705.0000f,
        99.6000f,
        0.0000f,
        0x1
    );

    /* Sports Presets */

    /// <summary>
    /// A reverb preset (approximating an empty sports stadium).
    /// </summary>
    public static readonly ReverbProperties SportEmptyStadium = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.4467f,
        0.7943f,
        6.2600f,
        0.5100f,
        1.1000f,
        0.0631f,
        0.1830f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.3981f,
        0.0380f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a squash court).
    /// </summary>
    public static readonly ReverbProperties SportSquashCourt = new ReverbProperties
    (
        1.0000f,
        0.7500f,
        0.3162f,
        0.3162f,
        0.7943f,
        2.2200f,
        0.9100f,
        1.1600f,
        0.4467f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7943f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1260f,
        0.1900f,
        0.2500f,
        0.0000f,
        0.9943f,
        7176.8999f,
        211.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a small swimming pool).
    /// </summary>
    public static readonly ReverbProperties SportSmallSwimmingPool = new ReverbProperties
    (
        1.0000f,
        0.7000f,
        0.3162f,
        0.7943f,
        0.8913f,
        2.7600f,
        1.2500f,
        1.1400f,
        0.6310f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7943f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1790f,
        0.1500f,
        0.8950f,
        0.1900f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a large swimming pool).
    /// </summary>
    public static readonly ReverbProperties SportLargeSwimmingPool = new ReverbProperties
    (
        1.0000f,
        0.8200f,
        0.3162f,
        0.7943f,
        1.0000f,
        5.4900f,
        1.3100f,
        1.1400f,
        0.4467f,
        0.0390f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.5012f,
        0.0490f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2220f,
        0.5500f,
        1.1590f,
        0.2100f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a gymnasium).
    /// </summary>
    public static readonly ReverbProperties SportGymnasium = new ReverbProperties
    (
        1.0000f,
        0.8100f,
        0.3162f,
        0.4467f,
        0.8913f,
        3.1400f,
        1.0600f,
        1.3500f,
        0.3981f,
        0.0290f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.5623f,
        0.0450f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1460f,
        0.1400f,
        0.2500f,
        0.0000f,
        0.9943f,
        7176.8999f,
        211.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a full stadium).
    /// </summary>
    public static readonly ReverbProperties SportFullStadium = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.0708f,
        0.7943f,
        5.2500f,
        0.1700f,
        0.8000f,
        0.1000f,
        0.1880f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2818f,
        0.0380f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a tannoy stadium).
    /// </summary>
    public static readonly ReverbProperties SportStadiumTannoy = new ReverbProperties
    (
        1.0000f,
        0.7800f,
        0.3162f,
        0.5623f,
        0.5012f,
        2.5300f,
        0.8800f,
        0.6800f,
        0.2818f,
        0.2300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.5012f,
        0.0630f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.2000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /* Prefab Presets */

    /// <summary>
    /// A reverb preset (approximating a workshop).
    /// </summary>
    public static readonly ReverbProperties PrefabWorkshop = new ReverbProperties
    (
        0.4287f,
        1.0000f,
        0.3162f,
        0.1413f,
        0.3981f,
        0.7600f,
        1.0000f,
        1.0000f,
        1.0000f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1220f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a school room).
    /// </summary>
    public static readonly ReverbProperties PrefabSchoolRoom = new ReverbProperties
    (
        0.4022f,
        0.6900f,
        0.3162f,
        0.6310f,
        0.5012f,
        0.9800f,
        0.4500f,
        0.1800f,
        1.4125f,
        0.0170f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.4125f,
        0.0150f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.0950f,
        0.1400f,
        0.2500f,
        0.0000f,
        0.9943f,
        7176.8999f,
        211.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a practise room).
    /// </summary>
    public static readonly ReverbProperties PrefabPractiseRoom = new ReverbProperties
    (
        0.4022f,
        0.8700f,
        0.3162f,
        0.3981f,
        0.5012f,
        1.1200f,
        0.5600f,
        0.1800f,
        1.2589f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.4125f,
        0.0110f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.0950f,
        0.1400f,
        0.2500f,
        0.0000f,
        0.9943f,
        7176.8999f,
        211.2000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an outhouse).
    /// </summary>
    public static readonly ReverbProperties PrefabOuthouse = new ReverbProperties
    (
        1.0000f,
        0.8200f,
        0.3162f,
        0.1122f,
        0.1585f,
        1.3800f,
        0.3800f,
        0.3500f,
        0.8913f,
        0.0240f,
        new Vector3(0.0000f, 0.0000f, -0.0000f),
        0.6310f,
        0.0440f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1210f,
        0.1700f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        107.5000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a caravan).
    /// </summary>
    public static readonly ReverbProperties PrefabCaravan = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.0891f,
        0.1259f,
        0.4300f,
        1.5000f,
        1.0000f,
        1.0000f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.9953f,
        0.0120f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /* Dome and Pipe Presets */

    /// <summary>
    /// A reverb preset (approximating a dome in a tomb).
    /// </summary>
    public static readonly ReverbProperties DomeTomb = new ReverbProperties
    (
        1.0000f,
        0.7900f,
        0.3162f,
        0.3548f,
        0.2239f,
        4.1800f,
        0.2100f,
        0.1000f,
        0.3868f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.6788f,
        0.0220f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1770f,
        0.1900f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        20.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a small pipe).
    /// </summary>
    public static readonly ReverbProperties PipeSmall = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.3548f,
        0.2239f,
        5.0400f,
        0.1000f,
        0.1000f,
        0.5012f,
        0.0320f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        2.5119f,
        0.0150f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        20.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating the dome in St. Paul's Cathedral, London).
    /// </summary>
    public static readonly ReverbProperties DomeSaintPauls = new ReverbProperties
    (
        1.0000f,
        0.8700f,
        0.3162f,
        0.3548f,
        0.2239f,
        10.4800f,
        0.1900f,
        0.1000f,
        0.1778f,
        0.0900f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0420f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.1200f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        20.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a long, thin pipe).
    /// </summary>
    public static readonly ReverbProperties PipeLongThin = new ReverbProperties
    (
        0.2560f,
        0.9100f,
        0.3162f,
        0.4467f,
        0.2818f,
        9.2100f,
        0.1800f,
        0.1000f,
        0.7079f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7079f,
        0.0220f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        20.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a large pipe).
    /// </summary>
    public static readonly ReverbProperties PipeLarge = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.3548f,
        0.2239f,
        8.4500f,
        0.1000f,
        0.1000f,
        0.3981f,
        0.0460f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.5849f,
        0.0320f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        20.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a resonant pipe).
    /// </summary>
    public static readonly ReverbProperties PipeResonant = new ReverbProperties
    (
        0.1373f,
        0.9100f,
        0.3162f,
        0.4467f,
        0.2818f,
        6.8100f,
        0.1800f,
        0.1000f,
        0.7079f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.0000f,
        0.0220f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        20.0000f,
        0.0000f,
        0x0
    );

    /* Outdoors Presets */

    /// <summary>
    /// A reverb preset (approximating an outdoors backyard).
    /// </summary>
    public static readonly ReverbProperties OutdoorsBackyard = new ReverbProperties
    (
        1.0000f,
        0.4500f,
        0.3162f,
        0.2512f,
        0.5012f,
        1.1200f,
        0.3400f,
        0.4600f,
        0.4467f,
        0.0690f,
        new Vector3(0.0000f, 0.0000f, -0.0000f),
        0.7079f,
        0.0230f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2180f,
        0.3400f,
        0.2500f,
        0.0000f,
        0.9943f,
        4399.1001f,
        242.9000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating rolling plains).
    /// </summary>
    public static readonly ReverbProperties OutdoorsRollingPlains = new ReverbProperties
    (
        1.0000f,
        0.0000f,
        0.3162f,
        0.0112f,
        0.6310f,
        2.1300f,
        0.2100f,
        0.4600f,
        0.1778f,
        0.3000f,
        new Vector3(0.0000f, 0.0000f, -0.0000f),
        0.4467f,
        0.0190f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        1.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4399.1001f,
        242.9000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a deep canyon).
    /// </summary>
    public static readonly ReverbProperties OutdoorsDeepCanyon = new ReverbProperties
    (
        1.0000f,
        0.7400f,
        0.3162f,
        0.1778f,
        0.6310f,
        3.8900f,
        0.2100f,
        0.4600f,
        0.3162f,
        0.2230f,
        new Vector3(0.0000f, 0.0000f, -0.0000f),
        0.3548f,
        0.0190f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        1.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        4399.1001f,
        242.9000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a creek).
    /// </summary>
    public static readonly ReverbProperties OutdoorsCreek = new ReverbProperties
    (
        1.0000f,
        0.3500f,
        0.3162f,
        0.1778f,
        0.5012f,
        2.1300f,
        0.2100f,
        0.4600f,
        0.3981f,
        0.1150f,
        new Vector3(0.0000f, 0.0000f, -0.0000f),
        0.1995f,
        0.0310f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2180f,
        0.3400f,
        0.2500f,
        0.0000f,
        0.9943f,
        4399.1001f,
        242.9000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a valley).
    /// </summary>
    public static readonly ReverbProperties OutdoorsValley = new ReverbProperties
    (
        1.0000f,
        0.2800f,
        0.3162f,
        0.0282f,
        0.1585f,
        2.8800f,
        0.2600f,
        0.3500f,
        0.1413f,
        0.2630f,
        new Vector3(0.0000f, 0.0000f, -0.0000f),
        0.3981f,
        0.1000f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.3400f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        107.5000f,
        0.0000f,
        0x0
    );

    /* Mood Presets */

    /// <summary>
    /// A reverb preset (approximating a heavenly mood).
    /// </summary>
    public static readonly ReverbProperties MoodHeaven = new ReverbProperties
    (
        1.0000f,
        0.9400f,
        0.3162f,
        0.7943f,
        0.4467f,
        5.0400f,
        1.1200f,
        0.5600f,
        0.2427f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0290f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0800f,
        2.7420f,
        0.0500f,
        0.9977f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a hellish mood).
    /// </summary>
    public static readonly ReverbProperties MoodHell = new ReverbProperties
    (
        1.0000f,
        0.5700f,
        0.3162f,
        0.3548f,
        0.4467f,
        3.5700f,
        0.4900f,
        2.0000f,
        0.0000f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.4125f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1100f,
        0.0400f,
        2.1090f,
        0.5200f,
        0.9943f,
        5000.0000f,
        139.5000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating an abstract memory).
    /// </summary>
    public static readonly ReverbProperties MoodMemory = new ReverbProperties
    (
        1.0000f,
        0.8500f,
        0.3162f,
        0.6310f,
        0.3548f,
        4.0600f,
        0.8200f,
        0.5600f,
        0.0398f,
        0.0000f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.1220f,
        0.0000f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.4740f,
        0.4500f,
        0.9886f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /* Driving Presets */

    /// <summary>
    /// A reverb preset (approximating a person in the commentator's seat).
    /// </summary>
    public static readonly ReverbProperties DrivingCommentator = new ReverbProperties
    (
        1.0000f,
        0.0000f,
        0.3162f,
        0.5623f,
        0.5012f,
        2.4200f,
        0.8800f,
        0.6800f,
        0.1995f,
        0.0930f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2512f,
        0.0170f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        1.0000f,
        0.2500f,
        0.0000f,
        0.9886f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a pit or garage).
    /// </summary>
    public static readonly ReverbProperties DrivingPitGarage = new ReverbProperties
    (
        0.4287f,
        0.5900f,
        0.3162f,
        0.7079f,
        0.5623f,
        1.7200f,
        0.9300f,
        0.8700f,
        0.5623f,
        0.0000f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0160f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.1100f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating driving in a race car).
    /// </summary>
    public static readonly ReverbProperties DrivingInCarRacer = new ReverbProperties
    (
        0.0832f,
        0.8000f,
        0.3162f,
        1.0000f,
        0.7943f,
        0.1700f,
        2.0000f,
        0.4100f,
        1.7783f,
        0.0070f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7079f,
        0.0150f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        10268.2002f,
        251.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating driving in a sports car).
    /// </summary>
    public static readonly ReverbProperties DrivingInCarSports = new ReverbProperties
    (
        0.0832f,
        0.8000f,
        0.3162f,
        0.6310f,
        1.0000f,
        0.1700f,
        0.7500f,
        0.4100f,
        1.0000f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.5623f,
        0.0000f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        10268.2002f,
        251.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating driving in a luxury car).
    /// </summary>
    public static readonly ReverbProperties DrivingInCarLuxury = new ReverbProperties
    (
        0.2560f,
        1.0000f,
        0.3162f,
        0.1000f,
        0.5012f,
        0.1300f,
        0.4100f,
        0.4600f,
        0.7943f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.5849f,
        0.0100f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        10268.2002f,
        251.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating driving on a course with a full grand stand).
    /// </summary>
    public static readonly ReverbProperties DrivingFullGrandStand = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        0.2818f,
        0.6310f,
        3.0100f,
        1.3700f,
        1.2800f,
        0.3548f,
        0.0900f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1778f,
        0.0490f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        10420.2002f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating an empty grand stand).
    /// </summary>
    public static readonly ReverbProperties DrivingEmptyGrandStand = new ReverbProperties
    (
        1.0000f,
        1.0000f,
        0.3162f,
        1.0000f,
        0.7943f,
        4.6200f,
        1.7500f,
        1.4000f,
        0.2082f,
        0.0900f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2512f,
        0.0490f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.0000f,
        0.9943f,
        10420.2002f,
        250.0000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating driving in a tunnel).
    /// </summary>
    public static readonly ReverbProperties DrivingTunnel = new ReverbProperties
    (
        1.0000f,
        0.8100f,
        0.3162f,
        0.3981f,
        0.8913f,
        3.4200f,
        0.9400f,
        1.3100f,
        0.7079f,
        0.0510f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7079f,
        0.0470f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2140f,
        0.0500f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        155.3000f,
        0.0000f,
        0x1
    );

    /* City Presets */

    /// <summary>
    /// A reverb preset (approximating city streets).
    /// </summary>
    public static readonly ReverbProperties CityStreets = new ReverbProperties
    (
        1.0000f,
        0.7800f,
        0.3162f,
        0.7079f,
        0.8913f,
        1.7900f,
        1.1200f,
        0.9100f,
        0.2818f,
        0.0460f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1995f,
        0.0280f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.2000f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a subway).
    /// </summary>
    public static readonly ReverbProperties CitySubway = new ReverbProperties
    (
        1.0000f,
        0.7400f,
        0.3162f,
        0.7079f,
        0.8913f,
        3.0100f,
        1.2300f,
        0.9100f,
        0.7079f,
        0.0460f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0280f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1250f,
        0.2100f,
        0.2500f,
        0.0000f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a museum).
    /// </summary>
    public static readonly ReverbProperties CityMuseum = new ReverbProperties
    (
        1.0000f,
        0.8200f,
        0.3162f,
        0.1778f,
        0.1778f,
        3.2800f,
        1.4000f,
        0.5700f,
        0.2512f,
        0.0390f,
        new Vector3(0.0000f, 0.0000f, -0.0000f),
        0.8913f,
        0.0340f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1300f,
        0.1700f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        107.5000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating a library).
    /// </summary>
    public static readonly ReverbProperties CityLibrary = new ReverbProperties
    (
        1.0000f,
        0.8200f,
        0.3162f,
        0.2818f,
        0.0891f,
        2.7600f,
        0.8900f,
        0.4100f,
        0.3548f,
        0.0290f,
        new Vector3(0.0000f, 0.0000f, -0.0000f),
        0.8913f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1300f,
        0.1700f,
        0.2500f,
        0.0000f,
        0.9943f,
        2854.3999f,
        107.5000f,
        0.0000f,
        0x0
    );

    /// <summary>
    /// A reverb preset (approximating an underpass).
    /// </summary>
    public static readonly ReverbProperties CityUnderpass = new ReverbProperties
    (
        1.0000f,
        0.8200f,
        0.3162f,
        0.4467f,
        0.8913f,
        3.5700f,
        1.1200f,
        0.9100f,
        0.3981f,
        0.0590f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.8913f,
        0.0370f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.1400f,
        0.2500f,
        0.0000f,
        0.9920f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating an abandoned location).
    /// </summary>
    public static readonly ReverbProperties CityAbandoned = new ReverbProperties
    (
        1.0000f,
        0.6900f,
        0.3162f,
        0.7943f,
        0.8913f,
        3.2800f,
        1.1700f,
        0.9100f,
        0.4467f,
        0.0440f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2818f,
        0.0240f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.2000f,
        0.2500f,
        0.0000f,
        0.9966f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /* Misc. Presets */

    /// <summary>
    /// A reverb preset (approximating a dusty room).
    /// </summary>
    public static readonly ReverbProperties DustyRoom = new ReverbProperties
    (
        0.3645f,
        0.5600f,
        0.3162f,
        0.7943f,
        0.7079f,
        1.7900f,
        0.3800f,
        0.2100f,
        0.5012f,
        0.0020f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.2589f,
        0.0060f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2020f,
        0.0500f,
        0.2500f,
        0.0000f,
        0.9886f,
        13046.0000f,
        163.3000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a chapel).
    /// </summary>
    public static readonly ReverbProperties Chapel = new ReverbProperties
    (
        1.0000f,
        0.8400f,
        0.3162f,
        0.5623f,
        1.0000f,
        4.6200f,
        0.6400f,
        1.2300f,
        0.4467f,
        0.0320f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.7943f,
        0.0490f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.2500f,
        0.0000f,
        0.2500f,
        0.1100f,
        0.9943f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x1
    );

    /// <summary>
    /// A reverb preset (approximating a small, water-filled room).
    /// </summary>
    public static readonly ReverbProperties SmallWaterRoom = new ReverbProperties
    (
        1.0000f,
        0.7000f,
        0.3162f,
        0.4477f,
        1.0000f,
        1.5100f,
        1.2500f,
        1.1400f,
        0.8913f,
        0.0200f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        1.4125f,
        0.0300f,
        new Vector3(0.0000f, 0.0000f, 0.0000f),
        0.1790f,
        0.1500f,
        0.8950f,
        0.1900f,
        0.9920f,
        5000.0000f,
        250.0000f,
        0.0000f,
        0x0
    );
}

/// <summary>
/// Defines a set of predefined reverb properties.
/// </summary>
public struct ReverbProperties
{
    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbDensity"/>.
    /// </summary>
    public float Density { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbDiffusion "/>.
    /// </summary>
    public float Diffusion { get; }

    /// <summary>
    /// Gets the preset value for <ReverbGainsee cref="EffectFloat.ReverbGain"/>.
    /// </summary>
    public float Gain { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbGainHF"/>.
    /// </summary>
    public float GainHF { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbGainLF"/>.
    /// </summary>
    public float GainLF { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbDecayTime"/>.
    /// </summary>
    public float DecayTime { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbDecayHFRatio"/>.
    /// </summary>
    public float DecayHFRatio { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbDecayLFRatio"/>.
    /// </summary>
    public float DecayLFRatio { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbReflectionsGain"/>.
    /// </summary>
    public float ReflectionsGain { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbReflectionsDelay"/>.
    /// </summary>
    public float ReflectionsDelay { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectVector3.EaxReverbReflectionsPan"/>.
    /// </summary>
    public Vector3 ReflectionsPan { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbLateReverbGain"/>.
    /// </summary>
    public float LateReverbGain { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbLateReverbDelay"/>.
    /// </summary>
    public float LateReverbDelay { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectVector3.EaxReverbLateReverbPan"/>.
    /// </summary>
    public Vector3 LateReverbPan { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbEchoTime"/>.
    /// </summary>
    public float EchoTime { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbEchoDepth"/>.
    /// </summary>
    public float EchoDepth { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbModulationTime"/>.
    /// </summary>
    public float ModulationTime { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbModulationDepth"/>.
    /// </summary>
    public float ModulationDepth { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbAirAbsorptionGainHF"/>.
    /// </summary>
    public float AirAbsorptionGainHF { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbHFReference"/>.
    /// </summary>
    public float HFReference { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.EaxReverbLFReference"/>.
    /// </summary>
    public float LFReference { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectFloat.ReverbRoomRolloffFactor"/>.
    /// </summary>
    public float RoomRolloffFactor { get; }

    /// <summary>
    /// Gets the preset value for <see cref="EffectInteger.ReverbDecayHFLimit"/>.
    /// </summary>
    public int DecayHFLimit { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReverbProperties"/> struct.
    /// </summary>
    /// <param name="density">See <see cref="Density"/>.</param>
    /// <param name="diffusion">See <see cref="Diffusion"/>.</param>
    /// <param name="gain">See <see cref="Gain"/>.</param>
    /// <param name="gainHF">See <see cref="GainHF"/>.</param>
    /// <param name="gainLF">See <see cref="GainLF"/>.</param>
    /// <param name="decayTime">See <see cref="DecayTime"/>.</param>
    /// <param name="decayHFRatio">See <see cref="DecayHFRatio"/>.</param>
    /// <param name="decayLFRatio">See <see cref="DecayLFRatio"/>.</param>
    /// <param name="reflectionsGain">See <see cref="ReflectionsGain"/>.</param>
    /// <param name="reflectionsDelay">See <see cref="ReflectionsDelay"/>.</param>
    /// <param name="reflectionsPan">See <see cref="ReflectionsPan"/>.</param>
    /// <param name="lateReverbGain">See <see cref="LateReverbGain"/>.</param>
    /// <param name="lateReverbDelay">See <see cref="LateReverbDelay"/>.</param>
    /// <param name="lateReverbPan">See <see cref="LateReverbPan"/>.</param>
    /// <param name="echoTime">See <see cref="EchoTime"/>.</param>
    /// <param name="echoDepth">See <see cref="EchoDepth"/>.</param>
    /// <param name="modulationTime">See <see cref="ModulationTime"/>.</param>
    /// <param name="modulationDepth">See <see cref="ModulationDepth"/>.</param>
    /// <param name="airAbsorptionGainHF">See <see cref="AirAbsorptionGainHF"/>.</param>
    /// <param name="hfReference">See <see cref="HFReference"/>.</param>
    /// <param name="lfReference">See <see cref="LFReference"/>.</param>
    /// <param name="roomRolloffFactor">See <see cref="RoomRolloffFactor"/>.</param>
    /// <param name="decayHFLimit">See <see cref="DecayHFLimit"/>.</param>
    public ReverbProperties
    (
        float density,
        float diffusion,
        float gain,
        float gainHF,
        float gainLF,
        float decayTime,
        float decayHFRatio,
        float decayLFRatio,
        float reflectionsGain,
        float reflectionsDelay,
        Vector3 reflectionsPan,
        float lateReverbGain,
        float lateReverbDelay,
        Vector3 lateReverbPan,
        float echoTime,
        float echoDepth,
        float modulationTime,
        float modulationDepth,
        float airAbsorptionGainHF,
        float hfReference,
        float lfReference,
        float roomRolloffFactor,
        int decayHFLimit
    )
    {
        Density = density;
        Diffusion = diffusion;
        Gain = gain;
        GainHF = gainHF;
        GainLF = gainLF;
        DecayTime = decayTime;
        DecayHFRatio = decayHFRatio;
        DecayLFRatio = decayLFRatio;
        ReflectionsGain = reflectionsGain;
        ReflectionsDelay = reflectionsDelay;
        ReflectionsPan = reflectionsPan;
        LateReverbGain = lateReverbGain;
        LateReverbDelay = lateReverbDelay;
        LateReverbPan = lateReverbPan;
        EchoTime = echoTime;
        EchoDepth = echoDepth;
        ModulationTime = modulationTime;
        ModulationDepth = modulationDepth;
        AirAbsorptionGainHF = airAbsorptionGainHF;
        HFReference = hfReference;
        LFReference = lfReference;
        RoomRolloffFactor = roomRolloffFactor;
        DecayHFLimit = decayHFLimit;
    }
}

#endregion

#region Ranges

// Effect parameter ranges and defaults.
public class EffectRanges
{
    // Standard reverb effect
    public const float ReverbMinDensity = 0f;
    public const float ReverbMaxDensity = 1f;
    public const float ReverbDefaultDensity = 1f;

    public const float ReverbMinDiffusion = 0f;
    public const float ReverbMaxDiffusion = 1f;
    public const float ReverbDefaultDiffusion = 1f;

    public const float ReverbMinGain = 0f;
    public const float ReverbMaxGain = 1f;
    public const float ReverbDefaultGain = 0.32f;

    public const float ReverbMinGainHF = 0f;
    public const float ReverbMaxGainHF = 1f;
    public const float ReverbDefaultGainHF = 0.89f;

    public const float ReverbMinDecayTime = 0.1f;
    public const float ReverbMaxDecayTime = 20f;
    public const float ReverbDefaultDecayTime = 1.49f;

    public const float ReverbMinDecayHFRatio = 0.1f;
    public const float ReverbMaxDecayHFRatio = 2f;
    public const float ReverbDefaultDecayHFRatio = 0.83f;

    public const float ReverbMinReflectionsGain = 0f;
    public const float ReverbMaxReflectionsGain = 3.16f;
    public const float ReverbDefaultReflectionsGain = 0.05f;

    public const float ReverbMinReflectionsDelay = 0f;
    public const float ReverbMaxReflectionsDelay = 0.3f;
    public const float ReverbDefaultReflectionsDelay = 0.007f;

    public const float ReverbMinLateReverbGain = 0f;
    public const float ReverbMaxLateReverbGain = 10f;
    public const float ReverbDefaultLateReverbGain = 1.26f;

    public const float ReverbMinLateReverbDelay = 0f;
    public const float ReverbMaxLateReverbDelay = 0.1f;
    public const float ReverbDefaultLateReverbDelay = 0.011f;

    public const float ReverbMinAirAbsorptionGainHF = 0.892f;
    public const float ReverbMaxAirAbsorptionGainHF = 1f;
    public const float ReverbDefaultAirAbsorptionGainHF = 0.994f;

    public const float ReverbMinRoomRolloffFactor = 0f;
    public const float ReverbMaxRoomRolloffFactor = 10f;
    public const float ReverbDefaultRoomRolloffFactor = 0f;

    public const int ReverbMinDecayHFLimit = 0; // AL_FALSE
    public const int ReverbMaxDecayHFLimit = 1; // AL_TRUE
    public const int ReverbDefaultDecayHFLimit = 1; // AL_TRUE

    // EAX reverb effect
    public const float EaxReverbMinDensity = 0f;
    public const float EaxReverbMaxDensity = 1f;
    public const float EaxReverbDefaultDensity = 1f;

    public const float EaxReverbMinDiffusion = 0f;
    public const float EaxReverbMaxDiffusion = 1f;
    public const float EaxReverbDefaultDiffusion = 1f;

    public const float EaxReverbMinGain = 0f;
    public const float EaxReverbMaxGain = 1f;
    public const float EaxReverbDefaultGain = 0.32f;

    public const float EaxReverbMinGainHF = 0f;
    public const float EaxReverbMaxGainHF = 1f;
    public const float EaxReverbDefaultGainHF = 0.89f;

    public const float EaxReverbMinGainLF = 0f;
    public const float EaxReverbMaxGainLF = 1f;
    public const float EaxReverbDefaultGainLF = 1f;

    public const float EaxReverbMinDecayTime = 0.1f;
    public const float EaxReverbMaxDecayTime = 20f;
    public const float EaxReverbDefaultDecayTime = 1.49f;

    public const float EaxReverbMinDecayHFRatio = 0.1f;
    public const float EaxReverbMaxDecayHFRatio = 2f;
    public const float EaxReverbDefaultDecayHFRatio = 0.83f;

    public const float EaxReverbMinDecayLFRatio = 0.1f;
    public const float EaxReverbMaxDecayLFRatio = 2f;
    public const float EaxReverbDefaultDecayLFRatio = 1f;

    public const float EaxReverbMinReflectionsGain = 0f;
    public const float EaxReverbMaxReflectionsGain = 3.16f;
    public const float EaxReverbDefaultReflectionsGain = 0.05f;

    public const float EaxReverbMinReflectionsDelay = 0f;
    public const float EaxReverbMaxReflectionsDelay = 0.3f;
    public const float EaxReverbDefaultReflectionsDelay = 0.007f;

    public const float EaxReverbDefaultReflectionsPanXYZ = 0f;

    public const float EaxReverbMinLateReverbGain = 0f;
    public const float EaxReverbMaxLateReverbGain = 10f;
    public const float EaxReverbDefaultLateReverbGain = 1.26f;

    public const float EaxReverbMinLateReverbDelay = 0f;
    public const float EaxReverbMaxLateReverbDelay = 0.1f;
    public const float EaxReverbDefaultLateReverbDelay = 0.011f;

    public const float EaxReverbDefaultLateReverbPanXYZ = 0f;

    public const float EaxReverbMinEchoTime = 0.075f;
    public const float EaxReverbMaxEchoTime = 0.25f;
    public const float EaxReverbDefaultEchoTime = 0.25f;

    public const float EaxReverbMinEchoDepth = 0f;
    public const float EaxReverbMaxEchoDepth = 1f;
    public const float EaxReverbDefaultEchoDepth = 0f;

    public const float EaxReverbMinModulationTime = 0.04f;
    public const float EaxReverbMaxModulationTime = 4f;
    public const float EaxReverbDefaultModulationTime = 0.25f;

    public const float EaxReverbMinModulationDepth = 0f;
    public const float EaxReverbMaxModulationDepth = 1f;
    public const float EaxReverbDefaultModulationDepth = 0f;

    public const float EaxReverbMinAirAbsorptionGainHF = 0.892f;
    public const float EaxReverbMaxAirAbsorptionGainHF = 1f;
    public const float EaxReverbDefaultAirAbsorptionGainHF = 0.994f;

    public const float EaxReverbMinHFReference = 1000f;
    public const float EaxReverbMaxHFReference = 20000f;
    public const float EaxReverbDefaultHFReference = 5000f;

    public const float EaxReverbMinLFReference = 20f;
    public const float EaxReverbMaxLFReference = 1000f;
    public const float EaxReverbDefaultLFReference = 250f;

    public const float EaxReverbMinRoomRolloffFactor = 0f;
    public const float EaxReverbMaxRoomRolloffFactor = 10f;
    public const float EaxReverbDefaultRoomRolloffFactor = 0f;

    public const int EaxReverbMinDecayHFLimit = 0; // AL_FALSE
    public const int EaxReverbMaxDecayHFLimit = 1; // AL_TRUE
    public const int EaxReverbDefaultDecayHFLimit = 1; // AL_TRUE

    // Chorus effect
    public const int ChorusWaveform_Sinusoid = 0;
    public const int ChorusWaveform_Triangle = 1;

    public const int ChorusMinWaveform = 0;
    public const int ChorusMaxWaveform = 1;
    public const int ChorusDefaultWaveform = 1;

    public const int ChorusMinPhase = -180;
    public const int ChorusMaxPhase = 180;
    public const int ChorusDefaultPhase = 90;

    public const float ChorusMinRate = 0f;
    public const float ChorusMaxRate = 10f;
    public const float ChorusDefaultRate = 1.1f;

    public const float ChorusMinDepth = 0f;
    public const float ChorusMaxDepth = 1f;
    public const float ChorusDefaultDepth = 0.1f;

    public const float ChorusMinFeedback = -1f;
    public const float ChorusMaxFeedback = 1f;
    public const float ChorusDefaultFeedback = 0.25f;

    public const float ChorusMinDelay = 0f;
    public const float ChorusMaxDelay = 0.016f;
    public const float ChorusDefaultDelay = 0.016f;

    // Distortion effect
    public const float DistortionMinEdge = 0f;
    public const float DistortionMaxEdge = 1f;
    public const float DistortionDefaultEdge = 0.2f;

    public const float DistortionMinGain = 0.01f;
    public const float DistortionMaxGain = 1f;
    public const float DistortionDefaultGain = 0.05f;

    public const float DistortionMinLowpassCutoff = 80f;
    public const float DistortionMaxLowpassCutoff = 24000f;
    public const float DistortionDefaultLowpassCutoff = 8000f;

    public const float DistortionMinEQCenter = 80f;
    public const float DistortionMaxEQCenter = 24000f;
    public const float DistortionDefaultEQCenter = 3600f;

    public const float DistortionMinEQBandwidth = 80f;
    public const float DistortionMaxEQBandwidth = 24000f;
    public const float DistortionDefaultEQBandwidth = 3600f;

    // Echo effect
    public const float EchoMinDelay = 0f;
    public const float EchoMaxDelay = 0.207f;
    public const float EchoDefaultDelay = 0.1f;

    public const float EchoMinLRDelay = 0f;
    public const float EchoMaxLRDelay = 0.404f;
    public const float EchoDefaultLRDelay = 0.1f;

    public const float EchoMinDamping = 0f;
    public const float EchoMaxDamping = 0.99f;
    public const float EchoDefaultDamping = 0.5f;

    public const float EchoMinFeedback = 0f;
    public const float EchoMaxFeedback = 1f;
    public const float EchoDefaultFeedback = 0.5f;

    public const float EchoMinSpread = -1f;
    public const float EchoMaxSpread = 1f;
    public const float EchoDefaultSpread = -1f;

    // Flanger effect
    public const int FlangerWaveform_Sinusoid = 0;
    public const int FlangerWaveform_Triangle = 1;

    public const int FlangerMinWaveform = 0;
    public const int FlangerMaxWaveform = 1;
    public const int FlangerDefaultWaveform = 1;

    public const int FlangerMinPhase = -180;
    public const int FlangerMaxPhase = 180;
    public const int FlangerDefaultPhase = 0;

    public const float FlangerMinRate = 0f;
    public const float FlangerMaxRate = 10f;
    public const float FlangerDefaultRate = 0.27f;

    public const float FlangerMinDepth = 0f;
    public const float FlangerMaxDepth = 1f;
    public const float FlangerDefaultDepth = 1f;

    public const float FlangerMinFeedback = -1f;
    public const float FlangerMaxFeedback = 1f;
    public const float FlangerDefaultFeedback = -0.5f;

    public const float FlangerMinDelay = 0f;
    public const float FlangerMaxDelay = 0.004f;
    public const float FlangerDefaultDelay = 0.002f;

    // Frequency shifter effect
    public const float FrequencyShifterMinFrequency = 0f;
    public const float FrequencyShifterMaxFrequency = 24000f;
    public const float FrequencyShifterDefaultFrequency = 0f;

    public const int FrequencyShifterMinLeftDirection = 0;
    public const int FrequencyShifterMaxLeftDirection = 2;
    public const int FrequencyShifterDefaultLeftDirection = 0;

    public const int FrequencyShifterDirection_Down = 0;
    public const int FrequencyShifterDirection_Up = 1;
    public const int FrequencyShifterDirection_Of = 2;

    public const int FrequencyShifterMinRight_Direction = 0;
    public const int FrequencyShifterMaxRight_Direction = 2;
    public const int FrequencyShifterDefaultRight_Direction = 0;

    // Vocal morpher effect
    public const int VocalMorpherMinPhonemeA = 0;
    public const int VocalMorpherMaxPhonemeA = 29;
    public const int VocalMorpherDefaultPhonemeA = 0;

    public const int VocalMorpherMinPhonemeACoarseTuning = -24;
    public const int VocalMorpherMaxPhonemeACoarseTuning = 24;
    public const int VocalMorpherDefaultPhonemeACoarseTuning = 0;

    public const int VocalMorpherMinPhonemeB = 0;
    public const int VocalMorpherMaxPhonemeB = 29;
    public const int VocalMorpherDefaultPhonemeB = 10;

    public const int VocalMorpherMinPhonemeBCoarseTuning = -24;
    public const int VocalMorpherMaxPhonemeBCoarseTuning = 24;
    public const int VocalMorpherDefaultPhonemeBCoarseTuning = 0;

    public const int VocalMorpherWaveform_Sinusoid = 0;
    public const int VocalMorpherWaveform_Triangle = 1;
    public const int VocalMorpherWaveform_Sawtooth = 2;

    public const int VocalMorpherMinWaveform = 0;
    public const int VocalMorpherMaxWaveform = 2;
    public const int VocalMorpherDefaultWaveform = 0;

    public const float VocalMorpherMinRate = 0f;
    public const float VocalMorpherMaxRate = 10f;
    public const float VocalMorpherDefaultRate = 1.41f;

    // Pitch shifter effect
    public const int PitchShifterMinCoarseTune = -12;
    public const int PitchShifterMaxCoarseTune = 12;
    public const int PitchShifterDefaultCoarseTune = 12;

    public const int PitchShifterMinFineTune = -50;
    public const int PitchShifterMaxFineTune = 50;
    public const int PitchShifterDefaultFineTune = 0;

    // Ring modulator effect
    public const float RingModulatorMinFrequency = 0f;
    public const float RingModulatorMaxFrequency = 8000f;
    public const float RingModulatorDefaultFrequency = 440f;

    public const float RingModulatorMinHighpassCutoff = 0f;
    public const float RingModulatorMaxHighpassCutoff = 24000f;
    public const float RingModulatorDefaultHighpassCutoff = 800f;

    public const int RingModulator_Sinusoid = 0;
    public const int RingModulator_Sawtooth = 1;
    public const int RingModulator_Square = 2;

    public const int RingModulatorMinWaveform = 0;
    public const int RingModulatorMaxWaveform = 2;
    public const int RingModulatorDefaultWaveform = 0;

    // Autowah effect
    public const float AutowahMinAttackTime = 0.0001f;
    public const float AutowahMaxAttackTime = 1f;
    public const float AutowahDefaultAttackTime = 0.06f;

    public const float AutowahMinReleaseTime = 0.0001f;
    public const float AutowahMaxReleaseTime = 1f;
    public const float AutowahDefaultReleaseTime = 0.06f;

    public const float AutowahMinResonance = 2f;
    public const float AutowahMaxResonance = 1000f;
    public const float AutowahDefaultResonance = 1000f;

    public const float AutowahMinPeakGain = 0.00003f;
    public const float AutowahMaxPeakGain = 31621f;
    public const float AutowahDefaultPeakGain = 11.22f;

    // Compressor effect
    public const int CompressorMinOnOff = 0;
    public const int CompressorMaxOnOff = 1;
    public const int CompressorDefaultOnOff = 1;

    // Equalizer effect
    public const float EqualizerMinLowGain = 0.126f;
    public const float EqualizerMaxLowGain = 7.943f;
    public const float EqualizerDefaultLowGain = 1f;

    public const float EqualizerMinLowCutoff = 50f;
    public const float EqualizerMaxLowCutoff = 800f;
    public const float EqualizerDefaultLowCutoff = 200f;

    public const float EqualizerMinMid1Gain = 0.126f;
    public const float EqualizerMaxMid1Gain = 7.943f;
    public const float EqualizerDefaultMid1Gain = 1f;

    public const float EqualizerMinMid1Center = 200f;
    public const float EqualizerMaxMid1Center = 3000f;
    public const float EqualizerDefaultMid1Center = 500f;

    public const float EqualizerMinMid1Width = 0.01f;
    public const float EqualizerMaxMid1Width = 1f;
    public const float EqualizerDefaultMid1Width = 1f;

    public const float EqualizerMinMid2Gain = 0.126f;
    public const float EqualizerMaxMid2Gain = 7.943f;
    public const float EqualizerDefaultMid2Gain = 1f;

    public const float EqualizerMinMid2Center = 1000f;
    public const float EqualizerMaxMid2Center = 8000f;
    public const float EqualizerDefaultMid2Center = 3000f;

    public const float EqualizerMinMid2Width = 0.01f;
    public const float EqualizerMaxMid2Width = 1f;
    public const float EqualizerDefaultMid2Width = 1f;

    public const float EqualizerMinHighGain = 0.126f;
    public const float EqualizerMaxHighGain = 7.943f;
    public const float EqualizerDefaultHighGain = 1f;

    public const float EqualizerMinHighCutoff = 4000f;
    public const float EqualizerMaxHighCutoff = 16000f;
    public const float EqualizerDefaultHighCutoff = 6000f;
}

// Filter ranges and defaults.
public class FilterRanges
{
    // Lowpass filter
    public const float LowpassMinGain = 0f;
    public const float LowpassMaxGain = 1f;
    public const float LowpassDefaultGain = 1f;

    public const float LowpassMinGainHF = 0f;
    public const float LowpassMaxGainHF = 1f;
    public const float LowpassDefaultGainHF = 1f;

    // Highpass filter
    public const float HighpassMinGain = 0f;
    public const float HighpassMaxGain = 1f;
    public const float HighpassDefaultGain = 1f;

    public const float HighpassMinGainLF = 0f;
    public const float HighpassMaxGainLF = 1f;
    public const float HighpassDefaultGainLF = 1f;

    // Bandpass filter
    public const float BandpassMinGain = 0f;
    public const float BandpassMaxGain = 1f;
    public const float BandpassDefaultGain = 1f;

    public const float BandpassMinGainHF = 0f;
    public const float BandpassMaxGainHF = 1f;
    public const float BandpassDefaultGainHF = 1f;

    public const float BandpassMinGainLF = 0f;
    public const float BandpassMaxGainLF = 1f;
    public const float BandpassDefaultGainLF = 1f;
}

// Listener parameter value ranges and defaults.
public class ListenerRanges
{
    public const float MinMetersPerUnit = float.MinValue;
    public const float MaxMetersPerUnit = float.MaxValue;
    public const float DefaultMetersPerUnit = 1f;
}

// Source parameter value ranges and defaults.
public class SourceRanges
{
    public const float MinAirAbsorptionFactor = 0f;
    public const float MaxAirAbsorptionFactor = 10f;
    public const float DefaultAirAbsorptionFactor = 0f;

    public const float MinRoomRolloffFactor = 0f;
    public const float MaxRoomRolloffFactor = 10f;
    public const float DefaultRoomRolloffFactor = 0f;

    public const float MinConeOuterGainHF = 0f;
    public const float MaxConeOuterGainHF = 1f;
    public const float DefaultConeOuterGainHF = 1f;

    public const int MinDirectFilterGainHFAuto = 0; // AL_FALSE;
    public const int MaxDirectFilterGainHFAuto = 1; // AL_TRUE;
    public const int DefaultDirectFilterGainHFAuto = 1; // AL_TRUE;

    public const int MinAuxiliarySendFilterGainAuto = 0; // AL_FALSE;
    public const int MaxAuxiliarySendFilterGainAuto = 1; // AL_TRUE;
    public const int DefaultAuxiliarySendFilterGainAuto = 1; // AL_TRUE;

    public const int MinAuxiliarySendFilterGainHFAuto = 0; // AL_FALSE;
    public const int MaxAuxiliarySendFilterGainHFAuto = 1; // AL_TRUE;
    public const int DefaultAuxiliarySendFilterGainHFAuto = 1; // AL_TRUE;
}

#endregion
