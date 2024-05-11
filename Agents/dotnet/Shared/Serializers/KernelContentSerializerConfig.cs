// Example of contents for KernelContentSerializers.cs

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.SemanticKernel;
using System;

namespace Shared.Serializers
{
    public static class KernelContentSerializerConfig
    {
        public static void RegisterSerializers()
        {
            BsonSerializer.RegisterSerializer(new TextContentSerializer());
            BsonSerializer.RegisterSerializer(new ImageContentSerializer());
        }

        public static void RegisterClassMaps()
        {
            BsonClassMap.RegisterClassMap<KernelContent>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });

            BsonClassMap.RegisterClassMap<TextContent>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("TextContent");
            });

            BsonClassMap.RegisterClassMap<ImageContent>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("ImageContent");
            });
        }
    }
}
