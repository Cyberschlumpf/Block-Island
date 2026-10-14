using System; using System.Collections.Generic;
public static class MinecraftBlockMapper {
 public static Block Map(BlockSet bs,string mc){if(bs==null)return null;string n=mc.StartsWith("minecraft:")?mc.Substring(10):mc;Block b=null;
  if(n.Contains("water")) b=Find(bs,"Water");
  else if(n.Contains("lava")) b=Find(bs,"Red","Orange","BI Orange");
  else if(n=="grass_block"||n=="dirt_path") b=Find(bs,"Grass","BI Grass Paver","Dirt");
  else if(n.Contains("dirt")||n.Contains("podzol")||n.Contains("mud")) b=Find(bs,"Dirt","Grass");
  else if(n.Contains("sandstone")) b=Find(bs,"BI Sandstone","Sand","Stone");
  else if(n.Contains("sand")||n.Contains("gravel")) b=Find(bs,"Sand","BI Pebble","Stone");
  else if(n.Contains("glass")) b=Find(bs,"Glass","BI Stained Glass","BI Glass Panel");
  else if(n.Contains("oak_log")||n.Contains("oak_wood")) b=Find(bs,"BI Oak Wood","Light Wood","Wood");
  else if(n.Contains("birch_log")||n.Contains("birch_wood")) b=Find(bs,"BI Birch Wood","Birch Wood","Light Wood");
  else if(n.Contains("spruce_log")||n.Contains("spruce_wood")||n.Contains("dark_oak_log")) b=Find(bs,"BI Pine Wood","Dark Wood","Wood");
  else if(n.Contains("log")||n.Contains("wood")||n.Contains("planks")) b=Find(bs,"Light Wood","BI Oak Wood","Wood");
  else if(n.Contains("oak_leaves")) b=Find(bs,"BI Oak Leaf","Green Leaf");
  else if(n.Contains("birch_leaves")) b=Find(bs,"BI Birch Leaf","Green Leaf");
  else if(n.Contains("spruce_leaves")||n.Contains("dark_oak_leaves")) b=Find(bs,"BI Pine Leaf","Green Leaf");
  else if(n.Contains("leaves")) b=Find(bs,"Green Leaf","BI Oak Leaf");
  else if(n.Contains("brick")) b=Find(bs,"BI Red Brick","BI Blue Brick","Stone");
  else if(n.Contains("snow")||n.Contains("quartz")||n.Contains("calcite")) b=Find(bs,"White","Snow","Stone");
  else if(n.Contains("ice")) b=Find(bs,"Glass","BI Stained Glass");
  else if(n.Contains("cactus")) b=Find(bs,"BI Cactus","Cactus");
  else if(n.Contains("poppy")||n.Contains("red_tulip")) b=Find(bs,"BI Poppy","BI Red Flower");
  else if(n.Contains("dandelion")) b=Find(bs,"BI Dandelion","Flower 1");
  else if(n.Contains("flower")||n.Contains("tulip")||n.Contains("orchid")) b=Find(bs,"BI Pink Flowers","Flower 1");
  else if(n.Contains("grass")||n.Contains("fern")) b=Find(bs,"BI Grass Tuft","Grass 1","Grass");
  else if(n.Contains("rail")) b=Find(bs,"Rail Straight","Stone");
  else if(n.Contains("stone")||n.Contains("ore")||n.Contains("deepslate")||n.Contains("cobble")||n.Contains("andesite")||n.Contains("granite")||n.Contains("diorite")||n.Contains("bedrock")) b=Find(bs,"Stone","Rock","BI Stone Paver");
  else if(n.Contains("red")||n.Contains("nether")||n.Contains("magma")) b=Find(bs,"BI Red","Red","BI Red Brick","Stone");
  else if(n.Contains("blue")||n.Contains("lapis")) b=Find(bs,"BI Blue","Blue","Stone");
  else if(n.Contains("green")||n.Contains("emerald")) b=Find(bs,"BI Green","Green","Stone");
  else if(n.Contains("orange")||n.Contains("copper")) b=Find(bs,"BI Orange","Orange","Stone");
  else b=Find(bs,"Stone","Rock","BI Stone Paver"); return b; }
 static Block Find(BlockSet bs,params string[] names){foreach(string n in names){Block b=bs.FindBlock(n);if(b!=null)return b;}return bs.Count>0?bs[0]:null;}
 public static BlockDirection Direction(Dictionary<string,string> s){if(s==null)return BlockDirection.FORWARD;string f;if(!s.TryGetValue("facing",out f))return BlockDirection.FORWARD; if(f=="north")return BlockDirection.BACKWARD;if(f=="south")return BlockDirection.FORWARD;if(f=="east")return BlockDirection.RIGHT;if(f=="west")return BlockDirection.LEFT;return BlockDirection.FORWARD;}
}
