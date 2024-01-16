using Occtoo.Generic.Inriver.Model.Enums;
using Occtoo.Generic.Inriver.Model.Settings;
using System.Collections.Generic;

namespace Occtoo.Generic.Debugger.Settings.Clients
{
    public class ClientSettingsExample
    {
        public static Inriver.Settings Create()
        {
            return new Inriver.Settings //Axel Arigato Test ID
            {
                Environment = "aae1371a-34cc-4965-a0ec-fb8dc75a9ac2", // Guid for your client, use in code when creating media id
                OcctooDataProviderId = "", // obtained through Occtoo Studio
                OcctooDataProviderSecret = "", // obtained through Occtoo Studio
                DefaultLanguage = "en",
                DocumentIdForbiddenChars = " ;_|+;_|/;_|.;_", // | separated tuple of chars to replace in ids 
                ExportSettings = new ExportSettings
                {
                    Entities = new List<EntitySettings>
                    {
                        /// Example setting for the entity Product in inRiver
                        /// Here we merge the product and items into one datasource in Occtoo
                        /// i.e flattening the information
                        new EntitySettings
                        {
                            Name = "Product",
                            DataSource = "productitems",
                            EntityIdAlias = "ProductEntityId",
                            UniqueIdFields = new List<string> {"ItemId"},
                            Type = EntityType.Entity,
                            ChildrenMerges = new List<MergeSettings>
                            {
                                new MergeSettings
                                {
                                    Name = "Item",
                                    Link = "ProductItem",
                                    DataSource = "productitems",
                                    Type = MergeType.Full,
                                    LinkUpdateType = LinkUpdateType.UpdateTarget
},
                                new MergeSettings
                                {
                                    Name = "Resource",
                                    Link = "ProductResource",
                                    DataSource = "media",
                                    Type = MergeType.Ids,
                                    PropertyAlias = "Media",
                                    LinkUpdateType = LinkUpdateType.UpdateSource
                                }
                            },
                            ParentsMerges = new List<MergeSettings>
                            {
                                new MergeSettings
                                {
                                    Name = "ChannelNode",
                                    Link = "ChannelNodeProducts",
                                    DataSource = "productitems",
                                    PropertyAlias = "CategoryIds",
                                    Type = MergeType.ParentIds,
                                    LinkUpdateType = LinkUpdateType.UpdateTarget
                                }
                            },
                            ExceptionFields = new List<ExceptionFieldSettings>
                            {
                                new ExceptionFieldSettings
                                {
                                    Id = "ProductType",
                                    Alias = "ProductTypeKey",
                                    Type = ExceptionFieldType.CvlKeys
                                }
                            }
                        },
                        /// Example setting for the entity Item in inRiver
                        /// Here we merge the product and items into one datasource in Occtoo
                        /// i.e flattening the information
                        /// Mirroring the settings for Products, so that if an Item is updated
                        /// it will create the same combined datasource as if the Product was updated
                        new EntitySettings
                        {
                            Name = "Item",
                            Type = EntityType.Entity,
                            EntityIdAlias = "ItemEntityId",
                            DataSource = "productitems",
                            UniqueIdFields = new List<string> { "ItemId" },
                            ParentsMerges = new List<MergeSettings>
                            {
                                new MergeSettings
                                {
                                    Name = "Product",
                                    Link = "ProductItem",
                                    DataSource = "productitems",
                                    Type = MergeType.Full,
                                    LinkUpdateType = LinkUpdateType.UpdateTarget
                                }
                            },
                            ChildrenMerges = new List<MergeSettings>
                            {
                                new MergeSettings
                                {
                                    Name = "Resource",
                                    Link = "ItemResource",
                                    DataSource = "media",
                                    Type = MergeType.Ids,
                                    PropertyAlias = "Media",
                                    LinkUpdateType = LinkUpdateType.UpdateSource
                                }
                            }
                        },
                        /// Example setting for the entity Resource in inRiver
                        new EntitySettings
                        {
                            Name = "Resource",
                            Type = EntityType.Media,
                            DataSource = "media",
                            EntityIdAlias = "ResourceId",
                            UpdateParentsOnChange = true,
                            UniqueIdFields = new List<string> { "ResourceFilename" },
                            ParentsMerges = new List<MergeSettings>
                            {
                                new MergeSettings
                                {
                                    Name = "Product",
                                    Link = "ProductResource",
                                    DataSource = "media",
                                    PropertyAlias = "ProductIds",
                                    Type = MergeType.None,
                                    LinkUpdateType = LinkUpdateType.None
                                }
                            }
                        },
                        /// Example setting for the entity ChannelNode in inRiver
                        /// sending it to the datasource categories in Occtoo
                        new EntitySettings
                        {
                            Name = "ChannelNode",
                            Type = EntityType.Entity,
                            DataSource = "categories",
                            EntityIdAlias = "CategoryEntityId",
                            UniqueIdFields = new List<string> { "CategoryEntityId" },
                            ParentsMerges = new List<MergeSettings>
                            {
                                new MergeSettings
                                {
                                    Name = "ChannelNode",
                                    Link = "ChannelNodeChannelNodes",
                                    DataSource = "categories",
                                    PropertyAlias = "ParentIds",
                                    Type = MergeType.ParentIds,
                                    LinkUpdateType = LinkUpdateType.UpdateTarget
                                }
                            },
                            ExceptionFields = new List<ExceptionFieldSettings>
                            {
                                new ExceptionFieldSettings
                                {
                                    Id = "ChannelId",
                                    Alias = "ChannelId",
                                    Type = ExceptionFieldType.ListOfChannels
                                }
                            }
                        },
                        /// Example setting for the entity Channel in inRiver
                        /// sending it to the datasource categories in Occtoo
                        new EntitySettings
                        {
                            Name = "Channel",
                            Type = EntityType.Entity,
                            DataSource = "categories",
                            EntityIdAlias = "CategoryEntityId",
                            UniqueIdFields = new List<string> { "CategoryEntityId" }
                        }
                    }
                }
            };
        }
    }
}
