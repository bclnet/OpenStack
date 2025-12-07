using OpenStack.Client;
using System;
using UnityEngine;
#pragma warning disable CS0649, CS0169, CS8500

namespace OpenStack;

public class UnityClientHost : MonoBehaviour, IClientHost {
    [field: SerializeField] public string Family { get; set; }
    [field: SerializeField] public Uri Game { get; set; }

    public void Dispose() {
        throw new NotImplementedException();
    }

    public void Run() => throw new NotSupportedException();

    public void SetClient(ClientBase client) {
        throw new NotImplementedException();
    }
}
