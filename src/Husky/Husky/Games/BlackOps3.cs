// ------------------------------------------------------------------------
// Husky - Call of Duty BSP Extractor
// Copyright (C) 2018 Philip/Scobalula
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ------------------------------------------------------------------------
using PhilLibX;
using PhilLibX.IO;
using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Husky
{
    /// <summary>
    /// Bo3 Logic
    /// </summary>
    public class BlackOps3
    {
        /// <summary>
        /// Bo3 GfxMap Asset (some pointers we skip over point to DirectX routines, etc. if that means anything to anyone)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct GfxMap
        {
            /// <summary>
            /// A pointer to the name of this GfxMap Asset
            /// </summary>
            public long NamePointer { get; set; }

            /// <summary>
            /// A pointer to the name of the map 
            /// </summary>
            public long MapNamePointer { get; set; }

            /// <summary>
            /// Unknown Data
            /// </summary>
            public long Padding { get; set; }

            /// <summary>
            /// Number of Surfaces
            /// </summary>
            public int SurfaceCount { get; set; }

            /// <summary>
            /// Unknown Bytes (Possibly counts, pointers, etc. for other data we don't care about)
            /// </summary>
            public fixed byte Padding1[0x24C];

            /// <summary>
            /// Number of Gfx Vertices (XYZ, etc.)
            /// </summary>
            public int GfxVertexCount { get; set; }

            /// <summary>
            /// Raw size of the Gfx Vertex Buffer
            /// </summary>
            public int GfxVertexBufferSize { get; set; }

            /// <summary>
            /// Pointer to the Gfx Vertex Data
            /// </summary>
            public long GfxVerticesPointer { get; set; }

            /// <summary>
            /// Unknown Bytes (more BSP data we probably don't care for)
            /// </summary>
            public fixed byte Padding2[8];

            /// <summary>
            /// Pointer to the Gfx Vertex Data (Normals, UVs, and Color)
            /// </summary>
            public long GfxVertexDataBufferSize { get; set; }

            /// <summary>
            /// Pointer to the Gfx Vertex Data (Normals, UVs, and Color)
            /// </summary>
            public long GfxVerticesDataPointer { get; set; }

            /// <summary>
            /// Unknown Bytes (more BSP data we probably don't care for)
            /// </summary>
            public fixed byte Padding4[8];

            /// <summary>
            /// Number of Gfx Indices (for Faces)
            /// </summary>
            public long GfxIndicesCount { get; set; }

            /// <summary>
            /// Pointer to the Gfx Index Data
            /// </summary>
            public long GfxIndicesPointer { get; set; }

            /// <summary>
            /// Points, etc.
            /// </summary>
            public fixed byte Padding5[0x54];

            /// <summary>
            /// Number of Static Models
            /// </summary>
            public int GfxStaticModelsCount { get; set; }

            /// <summary>
            /// Points, etc.
            /// </summary>
            public fixed byte Padding6[0x130];

            /// <summary>
            /// Pointer to the Gfx Index Data
            /// </summary>
            public long GfxSurfacesPointer { get; set; }

            /// <summary>
            /// Pointer to the Gfx Static Models
            /// </summary>
            public long GfxStaticModelsPointer { get; set; }

            public int LutMaterial { get; set; }
        }

        public unsafe struct CollisionMap
        {
            /// <summary>
            /// A pointer to the name of this GfxMap Asset
            /// </summary>
            public int NamePointer { get; set; }

        }

        /// <summary>
        /// Gfx Map Surface
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct GfxSurface
        {
            /// <summary>
            /// Unknown Bytes
            /// </summary>
            public fixed byte Padding[0xC];

            /// <summary>
            /// Offset of the vertex chunk within the vertex buffer 
            /// </summary>
            public int VertexBufferOffset { get; set; }

            /// <summary>
            /// Unknown Bytes
            /// </summary>
            public fixed byte Padding1[0xC];

            /// <summary>
            /// Offset of the vertex data chunk within the vertex buffer 
            /// </summary>
            public int VertexDataBufferOffset { get; set; }

            /// <summary>
            /// Base Vertex Index (this is what allows the GfxMap to have 65k+ verts with only 2 byte indices)
            /// </summary>
            public int VertexIndex { get; set; }

            /// <summary>
            /// Padding
            /// </summary>
            public int Padding2 { get; set; }

            /// <summary>
            /// Number of Vertices this surface has
            /// </summary>
            public ushort VertexCount { get; set; }

            /// <summary>
            /// Number of Faces this surface has
            /// </summary>
            public ushort FaceCount { get; set; }

            /// <summary>
            /// Base Face Index (this is what allows the GfxMap to have 65k+ faces with only 2 byte indices)
            /// </summary>
            public int FaceIndex { get; set; }

            /// <summary>
            /// Unknown Bytes
            /// </summary>
            public fixed byte Padding3[0x18];

            /// <summary>
            /// Pointer to the Material Asset of this Surface
            /// </summary>
            public long MaterialPointer { get; set; }

            /// <summary>
            /// Unknown Bytes
            /// </summary>
            public fixed byte Padding4[0x10];
        }

        /// <summary>
        /// Material Asset Info
        /// </summary>
        unsafe public struct Material
        {
            /// <summary>
            /// A pointer to the name of this material
            /// </summary>
            public long NamePointer { get; set; }

            /// <summary>
            /// Unknown Bytes (Flags, etc.)
            /// </summary>
            public fixed byte Padding[40];

            /// <summary>
            /// Pointers to different Material Settings
            /// </summary>
            public fixed long Settings[72];

            /// <summary>
            /// Number of Images this Material uses
            /// </summary>
            public byte ImageCount { get; set; }

            /// <summary>
            /// Unknown Bytes
            /// </summary>
            public fixed byte Padding2[7];

            /// <summary>
            /// A pointer to the TechSet this material uses
            /// </summary>
            public long TechniquePointer { get; set; }

            /// <summary>
            /// A pointer to this material's image table
            /// </summary>
            public long ImageTablePointer { get; set; }

            /// <summary>
            /// Unknown Pointer
            /// </summary>
            public long UnknownPointer { get; set; }

            /// <summary>
            /// Unknown Pointers/Nulls
            /// </summary>
            public fixed long Padding3[2];
        }

        /// <summary>
        /// Gfx Vertex 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct GfxVertex
        {
            /// <summary>
            /// X Position
            /// </summary>
            public float X { get; set; }

            /// <summary>
            /// Y Position
            /// </summary>
            public float Y { get; set; }

            /// <summary>
            /// Z Position
            /// </summary>
            public float Z { get; set; }
        }

        /// <summary>
        /// Gfx Vertex Data
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct GfxVertexData
        {
            /// <summary>
            /// RGBA Color
            /// </summary>
            public GfxColor Color { get; set; }

            /// <summary>
            /// U Texture Position
            /// </summary>
            public float U { get; set; }

            /// <summary>
            /// V Texture Position
            /// </summary>
            public float V { get; set; }

            /// <summary>
            /// Packed Vertex Normal (same as XModels)
            /// </summary>
            public PackedUnitVector Normal { get; set; }

            /// <summary>
            /// Unknown Bytes (probably tangent)
            /// </summary>
            public int Padding2 { get; set; }
        }

        /// <summary>
        /// Gfx Vertex 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct GfxWeirdVtx
        {
            /// <summary>
            /// X Position
            /// </summary>
            public float X { get; set; }

            /// <summary>
            /// Y Position
            /// </summary>
            public float Y { get; set; }

            /// <summary>
            /// Z Position
            /// </summary>
            public float Z { get; set; }
        }

        /// <summary>
        /// Gfx Static Model
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct GfxStaticModel
        {
            /// <summary>
            /// X Origin
            /// </summary>
            public float X { get; set; }

            /// <summary>
            /// Y Origin
            /// </summary>
            public float Y { get; set; }

            /// <summary>
            /// Z Origin
            /// </summary>
            public float Z { get; set; }

            /// <summary>
            /// 3x3 Rotation Matrix
            /// </summary>
            public fixed float Matrix[9];

            /// <summary>
            /// Model Scale 
            /// </summary>
            public float ModelScale { get; set; }

            /// <summary>
            /// Unknown Bytes
            /// </summary>
            public fixed byte Padding[0x24];

            /// <summary>
            /// Pointer to the XModel Asset
            /// </summary>
            public long ModelPointer { get; set; }

            /// <summary>
            /// Unknown Bytes
            /// </summary>
            public fixed byte UnknownBytes2[0x38];
        }

        /// <summary>
        /// Reads BSP Data
        /// </summary>
        public static void ExportBSPData(ProcessReader reader, long assetPoolsAddress, long assetSizesAddress, string gameType, Action<object> printCallback = null)
        {
            // Found her
            printCallback?.Invoke("Found supported game: Call of Duty: Black Ops 3");

            // Validate by XModel Name
            if (reader.ReadNullTerminatedString(reader.ReadInt64(reader.ReadInt64(reader.GetBaseAddress() + assetPoolsAddress + 0x80))) == "void")
            {
                // Load BSP Pools (they only have a size of 1 so we don't care about reading more than 1)
                var gfxMapAsset = reader.ReadStruct<GfxMap>(reader.ReadInt64(reader.GetBaseAddress() + assetPoolsAddress + 16 * 0x20));
                var mapEntsAsset = reader.ReadStruct<MapEnts64>(reader.ReadInt64(reader.GetBaseAddress() + assetPoolsAddress + 0x1E0));


                // Name
                string gfxMapName = reader.ReadNullTerminatedString(gfxMapAsset.NamePointer);
                string mapName = reader.ReadNullTerminatedString(gfxMapAsset.MapNamePointer);
                string mapEnt = reader.ReadNullTerminatedString(mapEntsAsset.MapData);

                // Verify a BSP is actually loaded (if in base menu, etc, no map is loaded)
                if (String.IsNullOrWhiteSpace(gfxMapName))
                {
                    printCallback?.Invoke("No BSP loaded. Enter Main Menu or a Map to load in the required assets.");
                }
                else
                {
                    // New IW Map
                    var mapFile = new IWMap();
                    // Print Info
                    printCallback?.Invoke(String.Format("Loaded Gfx Map     -   {0}", gfxMapName));
                    printCallback?.Invoke(String.Format("Loaded Map         -   {0}", mapName));
                    printCallback?.Invoke(String.Format("Vertex Count       -   {0}", gfxMapAsset.GfxVertexCount));
                    printCallback?.Invoke(String.Format("Indices Count      -   {0}", gfxMapAsset.GfxIndicesCount));
                    printCallback?.Invoke(String.Format("Surface Count      -   {0}", gfxMapAsset.SurfaceCount));
                    printCallback?.Invoke(String.Format("Model Count        -   {0}", gfxMapAsset.GfxStaticModelsCount));
                    // Build output Folder
                    string outputName = Path.Combine("exported_maps", "black_ops_3", gameType, mapName, mapName);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputName));

                    // Code to find mapEnt address offset
                    /*string outputOffsets = Path.Combine(Path.GetDirectoryName(outputName), "TestOffsets", "Test_Offset_");
                    Directory.CreateDirectory(Path.GetDirectoryName(outputOffsets));
                    for (int test_offset = 0; test_offset < 513; test_offset++)
                    {
                        for (int test_offset2 = 0; test_offset2 < 65; test_offset2++)
                        {
                            var testmapEntsAsset = reader.ReadStruct<MapEnts64>(reader.ReadInt64(reader.GetBaseAddress() + assetPoolsAddress + test_offset * 0x1) + test_offset2);
                            string testmapEnt = reader.ReadNullTerminatedString(testmapEntsAsset.MapData);
                            if (!String.IsNullOrWhiteSpace(testmapEnt))
                            {
                                printCallback?.Invoke(String.Format("PoolAddressOffset MapEnts {0}, {1}", test_offset, test_offset2));
                                
                                File.WriteAllText(outputOffsets + String.Format("GAME_OFFSET_{0}_OFFSET2_{1}.txt", test_offset, test_offset2), testmapEnt);
                            }
                        }
                    }*/

                    // Stop watch
                    var stopWatch = Stopwatch.StartNew();

                    // Read Vertices
                    printCallback?.Invoke("Parsing vertex data....");
                    var vertices = ReadGfxVertices(reader, gfxMapAsset.GfxVerticesPointer, gfxMapAsset.GfxVerticesDataPointer, (int)gfxMapAsset.GfxVertexCount);
                    printCallback?.Invoke(String.Format("Parsed vertex data in {0:0.00} seconds.", stopWatch.ElapsedMilliseconds / 1000.0));

                    // Reset timer
                    stopWatch.Restart();

                    // Read Indices
                    printCallback?.Invoke("Parsing surface indices....");
                    var indices = ReadGfxIndices(reader, gfxMapAsset.GfxIndicesPointer, (int)gfxMapAsset.GfxIndicesCount);
                    printCallback?.Invoke(String.Format("Parsed indices in {0:0.00} seconds.", stopWatch.ElapsedMilliseconds / 1000.0));

                    // Reset timer
                    stopWatch.Restart();

                    // Read Indices
                    printCallback?.Invoke("Parsing surfaces....");
                    var surfaces = ReadGfxSufaces(reader, gfxMapAsset.GfxSurfacesPointer, gfxMapAsset.SurfaceCount);
                    printCallback?.Invoke(String.Format("Parsed surfaces in {0:0.00} seconds.", stopWatch.ElapsedMilliseconds / 1000.0));

                    // Reset timer
                    stopWatch.Restart();

                    // Write OBJ
                    printCallback?.Invoke("Generating map files...");

                    // Create new OBJ
                    var obj = new WavefrontOBJ();

                    // Append Vertex Data
                    foreach (var vertex in vertices)
                    {
                        obj.Vertices.Add(vertex.Position);
                        obj.Normals.Add(vertex.Normal);
                        obj.UVs.Add(vertex.UV);
                    }

                    // Image Names (for Search String)
                    HashSet<string> imageNames = new HashSet<string>();

                    // Append Faces
                    foreach (var surface in surfaces)
                    {
                        // Create new Material
                        var material = ReadMaterial(reader, surface.MaterialPointer);
                        // Add to images
                        // printCallback?.Invoke(String.Format("Diffuse : {0}", material.DiffuseMap));

                        imageNames.Add(material.DiffuseMap);
                        imageNames.Add(material.NormalMap);
                        imageNames.Add(material.SpecularMap);
                        imageNames.Add(material.HeightMap);
                        imageNames.Add(material.EmissionMap);
                        imageNames.Add(material.OcclusionMap);
                        // Add it
                        obj.AddMaterial(material);
                        // Add points
                        for (ushort i = 0; i < surface.FaceCount; i++)
                        {
                            // Face Indices (Bo3 seems to be like Bo2, but it isn't padded, etc. so this seems to work)
                            var faceIndex1 = indices[i * 3 + surface.FaceIndex] + surface.VertexBufferOffset / 12;
                            var faceIndex2 = indices[i * 3 + surface.FaceIndex + 1] + surface.VertexBufferOffset / 12;
                            var faceIndex3 = indices[i * 3 + surface.FaceIndex + 2] + surface.VertexBufferOffset / 12;

                            // Validate unique points, and write to OBJ
                            if (faceIndex1 != faceIndex2 && faceIndex1 != faceIndex3 && faceIndex2 != faceIndex3)
                            {
                                // new Obj Face
                                var objFace = new WavefrontOBJ.Face(material.Name);

                                // Add points
                                objFace.Vertices[0] = new WavefrontOBJ.Face.Vertex(faceIndex1, faceIndex1, faceIndex1);
                                objFace.Vertices[2] = new WavefrontOBJ.Face.Vertex(faceIndex2, faceIndex2, faceIndex2);
                                objFace.Vertices[1] = new WavefrontOBJ.Face.Vertex(faceIndex3, faceIndex3, faceIndex3);

                                // Add to OBJ
                                obj.Faces.Add(objFace);
                            }
                        }
                    }

                    // Save it
                    obj.Save(outputName + ".obj");

                    // Build search strinmg
                    string searchString = "";

                    // Loop through images, and append each to the search string (for Wraith/Greyhound)
                    foreach (string imageName in imageNames)
                        searchString += String.Format("{0},", Path.GetFileNameWithoutExtension(imageName));

                    // Get dynamic models from Map Entities
                    List<IDictionary> MapEntities = CreateMapEntities(mapEnt);

                    // Create .JSON with XModel Data
                    List<IDictionary> ModelData = CreateXModelDictionary(reader, gfxMapAsset.GfxStaticModelsPointer, (int)gfxMapAsset.GfxStaticModelsCount, MapEntities);
                    string xmodeljson = JToken.FromObject(ModelData).ToString(Formatting.Indented);
                    File.WriteAllText(outputName + "_xmodels.json", xmodeljson);

                    // Loop through xmodels, and append each to the search string (for Wraith/Greyhound)
                    List<string> xmodelList = CreateXModelList(ModelData);

                    // Create .JSON with World settings
                    Dictionary<string, string> world_settings = ParseWorldSettings(mapEnt);
                    string worldsettingsjson = JToken.FromObject(world_settings).ToString(Formatting.Indented);
                    File.WriteAllText(outputName + "_worldsettings.json", worldsettingsjson);


                    // Dump it
                    File.WriteAllText(outputName + "_search_string.txt", searchString);
                    File.WriteAllText(outputName + "_mapEnts.txt", mapEnt);
                    File.WriteAllText(outputName + "_xmodelList.txt", String.Join(",", xmodelList.ToArray()));

                    // Read entities and dump to map
                    mapFile.Entities.AddRange(ReadStaticModels(reader, gfxMapAsset.GfxStaticModelsPointer, (int)gfxMapAsset.GfxStaticModelsCount));
                    mapFile.DumpToMap(outputName + ".map");

                    // Done
                    printCallback?.Invoke(String.Format("Generated files in {0:0.00} seconds.", stopWatch.ElapsedMilliseconds / 1000.0));

                }

            }
            else
            {
                printCallback?.Invoke("Call of Duty: Advanced Warfare is supported, but this EXE is not.");
            }
        }

        /// <summary>
        /// Reads Gfx Surfaces
        /// </summary>
        public static GfxSurface[] ReadGfxSufaces(ProcessReader reader, long address, int count)
        {
            // Preallocate array
            GfxSurface[] surfaces = new GfxSurface[count];

            // Loop number of indices we have
            for (int i = 0; i < count; i++)
                // Add it
                surfaces[i] = reader.ReadStruct<GfxSurface>(address + i * 96);

            // Done
            return surfaces;
        }


        /// <summary>
        /// Reads Gfx Vertex Indices
        /// </summary>
        public static ushort[] ReadGfxIndices(ProcessReader reader, long address, int count)
        {
            // Preallocate short array
            ushort[] indices = new ushort[count];
            // Read buffer
            var byteBuffer = reader.ReadBytes(address, count * 2);
            // Copy buffer 
            Buffer.BlockCopy(byteBuffer, 0, indices, 0, byteBuffer.Length);
            // Done
            return indices;
        }

        /// <summary>
        /// Reads Gfx Vertices
        /// </summary>
        public static Vertex[] ReadGfxVertices(ProcessReader reader, long positionAddress, long dataAddress, int count)
        {
            // Preallocate vertex array
            Vertex[] vertices = new Vertex[count];
            // Read buffer
            var gfxPositionsBuffer = reader.ReadBytes(positionAddress, count * 12);
            var gfxDataBuffer = reader.ReadBytes(dataAddress, count * 20);
            // Loop number of vertices we have
            for (int i = 0; i < count; i++)
            {
                // Read Structs
                var gfxVertex = ByteUtil.BytesToStruct<GfxVertex>(gfxPositionsBuffer, i * 12);
                var gfxData = ByteUtil.BytesToStruct<GfxVertexData>(gfxDataBuffer, i * 20);
                // Create new SEModel Vertex
                vertices[i] = new Vertex()
                {
                    // Set offset
                    Position = new Vector3(
                        gfxVertex.X * 2.54,
                        gfxVertex.Y * 2.54,
                        gfxVertex.Z * 2.54),
                    // Decode and set normal (from DTZxPorter - Wraith, same as XModels)
                    Normal = VertexNormalUnpacking.MethodD(gfxData.Normal),
                    // Set UV
                    UV = new Vector2(gfxData.U, 1 - gfxData.V)
                };
            }

            // Done
            return vertices;
        }

        /// <summary>
        /// Reads a material for the given surface and its associated images
        /// </summary>
        public static WavefrontOBJ.Material ReadMaterial(ProcessReader reader, long address)
        {
            // Read Material
            var material = reader.ReadStruct<Material>(address);
            // Create new OBJ Image
            var objMaterial = new WavefrontOBJ.Material(Path.GetFileNameWithoutExtension(reader.ReadNullTerminatedString(reader.ReadInt64(address)).Replace("*", "")));
            // Loop over images
            for (byte i = 0; i < material.ImageCount; i++)
            {
                // Read Material Image
                var materialImage = reader.ReadStruct<MaterialImage64B>(material.ImageTablePointer + i * Marshal.SizeOf<MaterialImage64B>());
                // Check for color map for now
                if (materialImage.SemanticHash == 0xA0AB1041)
                    objMaterial.DiffuseMap = reader.ReadNullTerminatedString(reader.ReadInt64(materialImage.ImagePointer + 0xF8));
                else if (materialImage.SemanticHash == 0x59D30D0F)
                    objMaterial.NormalMap = reader.ReadNullTerminatedString(reader.ReadInt64(materialImage.ImagePointer + 0xF8));
                else if (materialImage.SemanticHash == 0x34ECCCB3)
                    objMaterial.SpecularMap = reader.ReadNullTerminatedString(reader.ReadInt64(materialImage.ImagePointer + 0xF8));
                else if (materialImage.SemanticHash == 0x34D849D5)
                    objMaterial.HeightMap = reader.ReadNullTerminatedString(reader.ReadInt64(materialImage.ImagePointer + 0xF8));
                else if (materialImage.SemanticHash == 0x34614347)
                    objMaterial.EmissionMap = reader.ReadNullTerminatedString(reader.ReadInt64(materialImage.ImagePointer + 0xF8));
                else if (materialImage.SemanticHash == 0x6001F931)
                    objMaterial.OcclusionMap = reader.ReadNullTerminatedString(reader.ReadInt64(materialImage.ImagePointer + 0xF8));
            }
            // Done
            return objMaterial;
        }

        /// <summary>
        /// Reads Static Models
        /// </summary>
        public unsafe static List<IWMap.Entity> ReadStaticModels(ProcessReader reader, long address, int count)
        {
            // Resulting Entities
            List<IWMap.Entity> entities = new List<IWMap.Entity>(count);
            // Read buffer
            var byteBuffer = reader.ReadBytes(address, count * Marshal.SizeOf<GfxStaticModel>());
            // Loop number of models we have
            for (int i = 0; i < count; i++)
            {
                // Read Struct
                var staticModel = ByteUtil.BytesToStruct<GfxStaticModel>(byteBuffer, i * Marshal.SizeOf<GfxStaticModel>());
                // Model Name
                var modelName = reader.ReadNullTerminatedString(reader.ReadInt64(staticModel.ModelPointer));
                // Skip blank models
                if (modelName.StartsWith("*"))
                    continue;
                // New Matrix
                var matrix = new Rotation.Matrix();
                // Copy X Values
                matrix.Values[0] = staticModel.Matrix[0];
                matrix.Values[1] = staticModel.Matrix[1];
                matrix.Values[2] = staticModel.Matrix[2];
                // Copy Y Values
                matrix.Values[4] = staticModel.Matrix[3];
                matrix.Values[5] = staticModel.Matrix[4];
                matrix.Values[6] = staticModel.Matrix[5];
                // Copy Z Values
                matrix.Values[8] = staticModel.Matrix[6];
                matrix.Values[9] = staticModel.Matrix[7];
                matrix.Values[10] = staticModel.Matrix[8];
                // Convert to Euler
                var euler = matrix.ToEuler();
                // Add it
                entities.Add(IWMap.Entity.CreateMiscModel(modelName, new Vector3(staticModel.X, staticModel.Y, staticModel.Z), Rotation.ToDegrees(euler), staticModel.ModelScale));
            }
            // Done
            return entities;
        }

        public unsafe static List<IDictionary> CreateMapEntities(string mapEnts)
        {
            List<string> DynModels = new List<string>();
            string[] Entities = mapEnts.Split(new[] { "\n}\n{" }, StringSplitOptions.None);
            foreach (string i in Entities)
            {
                if (i.Contains("script_model") && i.Contains("\"model\""))
                {
                    if (i.Contains("\"hq\"") == false && i.Contains("\"sab\"") == false && i.Contains("\"ctf\"") == false && i.Contains("\"sd\"") == false && i.Contains("\"special") == false)
                    {
                        DynModels.Add(i);
                    }
                }
            }

            List<IDictionary> ParsedList = new List<IDictionary>();
            Regex reg = new Regex(@"""(.*?)""\s""(.*?)""");

            foreach (string entity in DynModels)
            {
                string[] entity_properties = entity.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, string> model_data = new Dictionary<string, string>();
                foreach (String line in entity_properties)
                {
                    MatchCollection matches = reg.Matches(line);
                    foreach (Match m in matches)
                    {
                        if (m.Groups[1].Value == "model")
                        {
                            model_data.Add("Name", m.Groups[2].Value);
                        }
                        else if (m.Groups[1].Value == "origin")
                        {
                            string[] vec3 = m.Groups[2].Value.Split(new[] { " " }, StringSplitOptions.None);
                            model_data.Add("PosX", vec3[0]);
                            model_data.Add("PosY", vec3[1]);
                            model_data.Add("PosZ", vec3[2]);
                        }
                        else if (m.Groups[1].Value == "angles")
                        {
                            if (m.Groups[2].Value.Contains("e") == false)
                            {
                                string[] vec3 = m.Groups[2].Value.Split(new[] { " " }, StringSplitOptions.None);
                                model_data.Add("RotX", vec3[2]);
                                model_data.Add("RotY", vec3[0]);
                                model_data.Add("RotZ", vec3[1]);
                            }
                        }
                    }
                }
                model_data.Add("Scale", "1.0000");
                ParsedList.Add(model_data);
            }

            return ParsedList;
        }

        public unsafe static List<IDictionary> CreateXModelDictionary(ProcessReader reader, long address, int count, List<IDictionary> MapEntities)
        {
            // Force english-US input
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            // Read buffer
            var byteBuffer = reader.ReadBytes(address, count * Marshal.SizeOf<GfxStaticModel>());

            // Loop number of models we have
            List<IDictionary> MapModels = new List<IDictionary>(count);

            // Create list with unnessecary strings in model names
            List<string> badStrings = new List<string>()
            {
                "\"ml",
                "\"mv",
            };

            for (int i = 0; i < count; i++)
            {
                Dictionary<string, string> ModelData = new Dictionary<string, string>();
                List<double> Position = new List<double>();
                List<double> angles = new List<double>();

                // Read Struct
                var staticModel = ByteUtil.BytesToStruct<GfxStaticModel>(byteBuffer, i * Marshal.SizeOf<GfxStaticModel>());
                // Model Name
                var modelName = reader.ReadNullTerminatedString(reader.ReadInt64(staticModel.ModelPointer));

                var matrix = new Rotation.Matrix();
                // Copy X Values
                matrix.Values[0] = staticModel.Matrix[0];
                matrix.Values[1] = staticModel.Matrix[1];
                matrix.Values[2] = staticModel.Matrix[2];
                // Copy Y Values
                matrix.Values[4] = staticModel.Matrix[3];
                matrix.Values[5] = staticModel.Matrix[4];
                matrix.Values[6] = staticModel.Matrix[5];
                // Copy Z Values
                matrix.Values[8] = staticModel.Matrix[6];
                matrix.Values[9] = staticModel.Matrix[7];
                matrix.Values[10] = staticModel.Matrix[8];
                // Convert to Euler
                var euler = matrix.ToEuler();
                // Add it
                if (string.IsNullOrEmpty(modelName) == true || modelName.Contains("?") == true || modelName.Contains("'") == true || modelName.Contains("\\") == true || modelName.Contains("fx") == true || modelName.Contains("viewmodel") == true || modelName.Contains("*") == true || staticModel.ModelScale < 0.001 || staticModel.ModelScale > 10)
                {

                }
                else
                {
                    foreach (string b in badStrings)
                    {
                        if (modelName.Contains(b))
                        {
                            modelName = modelName.Replace(b, "\"");
                        }
                    }

                    ModelData.Add("Name", CleanInput(modelName));
                    ModelData.Add("PosX", string.Format("{0:0.0000}", staticModel.X));
                    ModelData.Add("PosY", string.Format("{0:0.0000}", staticModel.Y));
                    ModelData.Add("PosZ", string.Format("{0:0.0000}", staticModel.Z));
                    ModelData.Add("RotX", string.Format("{0:0.0000}", (float)Rotation.ToDegrees(euler).X).ToString(CultureInfo.InvariantCulture));
                    ModelData.Add("RotY", string.Format("{0:0.0000}", (float)Rotation.ToDegrees(euler).Y).ToString(CultureInfo.InvariantCulture));
                    ModelData.Add("RotZ", string.Format("{0:0.0000}", (float)Rotation.ToDegrees(euler).Z).ToString(CultureInfo.InvariantCulture));
                    ModelData.Add("Scale", string.Format("{0:0.0000}", staticModel.ModelScale).ToString(CultureInfo.InvariantCulture));
                    MapModels.Add(new Dictionary<string, string>(ModelData));
                }
            }
            foreach (IDictionary entity in MapEntities)
            {

                MapModels.Add(entity);
            }
            // Done
            return MapModels;
        }

        public unsafe static string CleanInput(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"[^\w\.@-]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        public unsafe static List<string> CreateXModelList(List<IDictionary> ModelData)
        {
            List<string> xmodel_list = new List<string>();
            int i = 0;
            foreach (Dictionary<string, string> model_dict in ModelData)
            {
                foreach (KeyValuePair<string, string> kvp in model_dict)
                {
                    if (kvp.Key == "Name" && xmodel_list.Contains(kvp.Value) == false)
                    {
                        xmodel_list.Add(kvp.Value);
                        i++;
                    }
                }
            }
            xmodel_list.Add(i.ToString());
            // Done
            return xmodel_list;
        }

        public unsafe static Dictionary<string, string> ParseWorldSettings(string mapEnts)
        {
            Regex reg = new Regex(@"""(.*?)""\s""(.*?)""");
            string world = mapEnts.Split(new[] { "\n}\n{" }, StringSplitOptions.None)[0];
            string[] world_settings = world.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, string> world_data = new Dictionary<string, string>();
            foreach (String line in world_settings)
            {
                MatchCollection matches = reg.Matches(line);
                foreach (Match m in matches)
                {
                    world_data.Add(m.Groups[1].Value, m.Groups[2].Value);
                }
            }
            return world_data;
        }
    }
}