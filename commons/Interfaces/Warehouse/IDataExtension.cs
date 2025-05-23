﻿using ICities;
using System;

namespace Klyte.Commons.Interfaces
{
    public interface IDataExtension
    {
        string SaveId { get; }

        void LoadDefaults(ISerializableData serializableData);
        IDataExtension Deserialize(Type type, byte[] data);
        byte[] Serialize();
        void OnReleased();
    }
}
