﻿using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.IO;

namespace WinmdUtils
{
    public class WinmdUtils : IDisposable
    {
        private FileStream stream;
        private PEReader peReader;
        private MetadataReader metadataReader;

        private WinmdUtils(string fileName)
        {
            this.stream = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            this.peReader = new PEReader(stream);
            this.metadataReader = this.peReader.GetMetadataReader();
        }

        public static WinmdUtils LoadFromFile(string fileName)
        {
            return new WinmdUtils(fileName);
        }

        public void Dispose()
        {
            this.stream?.Dispose();
            this.peReader?.Dispose();
        }

        public IEnumerable<DllImport> GetDllImports()
        {
            foreach (var methodDefHandle in this.metadataReader.MethodDefinitions)
            {
                var method = this.metadataReader.GetMethodDefinition(methodDefHandle);
                if (method.Attributes.HasFlag(System.Reflection.MethodAttributes.PinvokeImpl))
                {
                    var name = this.metadataReader.GetString(method.Name);
                    var import = method.GetImport();
                    var moduleRef = this.metadataReader.GetModuleReference(import.Module);
                    var dllName = this.metadataReader.GetString(moduleRef.Name);
                    yield return new DllImport(name, dllName);
                }
            }
        }
    }

    public class DllImport
    {
        public DllImport(string name, string dll)
        {
            this.Name = name;
            this.Dll = dll;
        }

        public string Name { get; private set; }

        public string Dll { get; private set; }

        public override string ToString()
        {
            return $"{this.Name}({this.Dll})";
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }
}
