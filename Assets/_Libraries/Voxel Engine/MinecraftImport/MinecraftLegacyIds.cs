using System.Collections.Generic;

// Minecraft 1.13+ block names -> pre-1.13 numeric id/meta. The Block Island native world renderer
// (TanviirImportWorld.Map) already has a detailed mapping for those legacy ids, so modern worlds are
// converted to them. Pure string logic, safe to call from worker threads.
public static class MinecraftLegacyIds {
    public const int Skip = -1;   // decorative detail without a full-block equivalent (signs, buttons, redstone dust …)

    static readonly string[] Colors = { "white","orange","magenta","light_blue","yellow","lime","pink","gray","light_gray","cyan","purple","blue","brown","green","red","black" };
    static readonly string[] Woods = { "oak","spruce","birch","jungle","acacia","dark_oak","crimson","warped","mangrove","cherry","bamboo" };

    static readonly Dictionary<string,int> exact = new Dictionary<string,int>() {
        {"stone",L(1)},{"granite",L(1,1)},{"polished_granite",L(1,2)},{"diorite",L(1,3)},{"polished_diorite",L(1,4)},{"andesite",L(1,5)},{"polished_andesite",L(1,6)},
        {"smooth_stone",L(1)},{"deepslate",L(1)},{"cobbled_deepslate",L(4)},{"polished_deepslate",L(1,6)},{"deepslate_bricks",L(98)},{"deepslate_tiles",L(98)},{"tuff",L(1,5)},{"calcite",L(1,4)},
        {"grass_block",L(2)},{"dirt",L(3)},{"coarse_dirt",L(3,1)},{"podzol",L(3,2)},{"rooted_dirt",L(3)},{"mud",L(3)},{"dirt_path",L(2)},{"grass_path",L(2)},{"farmland",L(60)},{"mycelium",L(110)},
        {"cobblestone",L(4)},{"mossy_cobblestone",L(48)},{"bedrock",L(7)},{"sand",L(12)},{"red_sand",L(12,1)},{"gravel",L(13)},{"clay",L(82)},
        {"water",L(9)},{"bubble_column",L(9)},{"kelp",L(9)},{"kelp_plant",L(9)},{"seagrass",L(9)},{"tall_seagrass",L(9)},{"lava",L(10)},
        {"gold_ore",L(14)},{"iron_ore",L(15)},{"coal_ore",L(16)},{"lapis_ore",L(21)},{"diamond_ore",L(56)},{"redstone_ore",L(73)},{"emerald_ore",L(129)},{"copper_ore",L(15)},
        {"nether_gold_ore",L(14)},{"nether_quartz_ore",L(153)},{"ancient_debris",L(87)},
        {"gold_block",L(41)},{"iron_block",L(42)},{"diamond_block",L(57)},{"lapis_block",L(22)},{"emerald_block",L(133)},{"redstone_block",L(152)},{"coal_block",L(173)},
        {"copper_block",L(42)},{"raw_iron_block",L(42)},{"raw_gold_block",L(41)},{"raw_copper_block",L(42)},{"netherite_block",L(49)},{"amethyst_block",L(57)},
        {"quartz_block",L(155)},{"chiseled_quartz_block",L(155)},{"quartz_pillar",L(155)},{"smooth_quartz",L(155)},{"quartz_bricks",L(155)},
        {"sponge",L(19)},{"wet_sponge",L(19)},{"glass",L(20)},{"tinted_glass",L(20)},{"glass_pane",L(102)},
        {"sandstone",L(24)},{"chiseled_sandstone",L(24,1)},{"cut_sandstone",L(24,2)},{"smooth_sandstone",L(24,2)},
        {"red_sandstone",L(24)},{"chiseled_red_sandstone",L(24,1)},{"cut_red_sandstone",L(24,2)},{"smooth_red_sandstone",L(24,2)},
        {"bricks",L(45)},{"stone_bricks",L(98)},{"mossy_stone_bricks",L(98,1)},{"cracked_stone_bricks",L(98,2)},{"chiseled_stone_bricks",L(98,3)},
        {"infested_stone",L(1)},{"infested_cobblestone",L(4)},{"infested_stone_bricks",L(98)},
        {"bookshelf",L(47)},{"tnt",L(46)},{"obsidian",L(49)},{"crying_obsidian",L(49)},{"spawner",L(52)},{"crafting_table",L(58)},
        {"furnace",L(61)},{"blast_furnace",L(61)},{"smoker",L(61)},{"chest",L(54)},{"trapped_chest",L(146)},{"barrel",L(54)},{"ender_chest",L(130)},
        {"ice",L(79)},{"packed_ice",L(174)},{"blue_ice",L(174)},{"frosted_ice",L(79)},{"snow_block",L(80)},{"snow",L(78)},{"powder_snow",L(80)},
        {"cactus",L(81)},{"sugar_cane",L(83)},{"bamboo",L(83)},{"jukebox",L(84)},{"note_block",L(25)},{"pumpkin",L(86)},{"carved_pumpkin",L(86)},{"jack_o_lantern",L(91)},{"melon",L(103)},
        {"netherrack",L(87)},{"soul_sand",L(88)},{"soul_soil",L(88)},{"glowstone",L(89)},{"shroomlight",L(89)},{"magma_block",L(87)},
        {"nether_bricks",L(112)},{"red_nether_bricks",L(112)},{"cracked_nether_bricks",L(112)},{"chiseled_nether_bricks",L(112)},{"nether_wart_block",L(112)},{"warped_wart_block",L(18)},
        {"basalt",L(49)},{"polished_basalt",L(49)},{"smooth_basalt",L(49)},{"blackstone",L(49)},{"polished_blackstone",L(49)},{"polished_blackstone_bricks",L(49)},{"gilded_blackstone",L(49)},
        {"end_stone",L(121)},{"end_stone_bricks",L(121)},{"purpur_block",L(155)},{"purpur_pillar",L(155)},
        {"prismarine",L(168)},{"prismarine_bricks",L(168)},{"dark_prismarine",L(168)},{"sea_lantern",L(169)},
        {"redstone_lamp",L(123)},{"hay_block",L(170)},{"bone_block",L(155)},{"dried_kelp_block",L(170)},{"slime_block",L(165)},{"honey_block",L(165)},
        {"terracotta",L(172)},{"cobweb",L(30)},{"vine",L(106)},{"lily_pad",L(111)},{"dead_bush",L(32)},
        {"grass",L(31,1)},{"short_grass",L(31,1)},{"fern",L(31,2)},{"tall_grass",L(175,2)},{"large_fern",L(175,3)},
        {"dandelion",L(37)},{"poppy",L(38)},{"blue_orchid",L(38,1)},{"allium",L(38,2)},{"azure_bluet",L(38,3)},{"red_tulip",L(38,4)},{"orange_tulip",L(38,5)},
        {"white_tulip",L(38,6)},{"pink_tulip",L(38,7)},{"oxeye_daisy",L(38,8)},{"cornflower",L(38,1)},{"lily_of_the_valley",L(38,6)},
        {"sunflower",L(175)},{"lilac",L(175,1)},{"rose_bush",L(175,4)},{"peony",L(175,5)},
        {"brown_mushroom",L(39)},{"red_mushroom",L(40)},{"brown_mushroom_block",L(99)},{"red_mushroom_block",L(100)},{"mushroom_stem",L(99)},
        {"wheat",L(59)},{"carrots",L(141)},{"potatoes",L(142)},{"beetroots",L(141)},
        {"rail",L(66)},{"powered_rail",L(27)},{"detector_rail",L(28)},{"activator_rail",L(157)},
        {"torch",L(50)},{"wall_torch",L(50)},{"ladder",L(65)},{"iron_door",L(71)},{"iron_bars",L(101)},{"chain",L(101)},{"iron_trapdoor",L(167)},
        {"anvil",L(145)},{"chipped_anvil",L(145)},{"damaged_anvil",L(145)},{"enchanting_table",L(116)},{"brewing_stand",L(117)},{"cauldron",L(118)},{"water_cauldron",L(118)},
        {"hopper",L(154)},{"beacon",L(138)},{"dispenser",L(23)},{"dropper",L(23)},{"piston",L(33)},{"sticky_piston",L(29)},{"observer",L(33)},
        {"end_portal_frame",L(120)},{"nether_portal",L(90)},{"scaffolding",L(85)},
    };

    // Small decorative details: leaving them out looks better than turning them into full blocks.
    static readonly HashSet<string> skip = new HashSet<string>() {
        "redstone_wire","tripwire","tripwire_hook","lever","flower_pot","repeater","comparator","daylight_detector","end_rod","sea_pickle",
        "barrier","structure_void","light","string","lantern","soul_lantern","soul_torch","soul_wall_torch","redstone_torch","redstone_wall_torch",
        "moving_piston","piston_head","fire","soul_fire","conduit","bell","grindstone","stonecutter","lectern","loom","cartography_table",
        "fletching_table","smithing_table","composter","campfire","soul_campfire","turtle_egg","candle","glow_lichen","pointed_dripstone",
        "spore_blossom","small_dripleaf","big_dripleaf","big_dripleaf_stem","hanging_roots","moss_carpet","azalea","flowering_azalea","cocoa"
    };

    static int L(int id, int meta = 0) { return (id << 4) | (meta & 15); }

    // Returns false for air. id == Skip means: leave this voxel empty.
    public static bool Convert(string fullName, IDictionary<string,string> props, out int id, out int meta) {
        id = 0; meta = 0;
        string n = fullName.StartsWith("minecraft:") ? fullName.Substring(10) : fullName;
        if(n == "air" || n == "cave_air" || n == "void_air") return false;
        int v;
        if(skip.Contains(n) || n.EndsWith("_button") || n.EndsWith("_pressure_plate") || n.EndsWith("_sign") || n.EndsWith("_banner")
           || n.StartsWith("potted_") || n.EndsWith("_head") || n.EndsWith("_skull") || n.EndsWith("_candle") || n.EndsWith("_coral_fan")
           || n.EndsWith("_coral_wall_fan") || n.EndsWith("_bed") || n.EndsWith("_carpet") && n.StartsWith("moss")) { id = Skip; return true; }
        if(exact.TryGetValue(n, out v)) { Split(v, out id, out meta); return true; }

        // colour families
        for(int c = 0; c < Colors.Length; c++) {
            string pre = Colors[c] + "_";
            if(!n.StartsWith(pre)) continue;
            string rest = n.Substring(pre.Length);
            if(rest == "wool") { id = 35; meta = c; return true; }
            if(rest == "stained_glass") { id = 95; meta = c; return true; }
            if(rest == "stained_glass_pane") { id = 160; meta = c; return true; }
            if(rest == "terracotta" || rest == "glazed_terracotta" || rest == "concrete" || rest == "concrete_powder" || rest == "shulker_box") { id = 159; meta = c; return true; }
            if(rest == "carpet") { id = 171; meta = c; return true; }
            if(rest == "bed") { id = Skip; return true; }
        }

        // wood families
        for(int w = 0; w < Woods.Length; w++) {
            string wood = Woods[w];
            if(!n.Contains(wood)) continue;
            int legacy = w <= 5 ? w : (wood == "warped" || wood == "mangrove" ? 1 : 5);   // newer woods: closest legacy colour
            string rest = n.Replace("stripped_", "");
            if(rest == wood + "_planks") { id = 5; meta = legacy; return true; }
            if(rest == wood + "_log" || rest == wood + "_wood" || rest == wood + "_stem" || rest == wood + "_hyphae") {
                if(legacy <= 3) { id = 17; meta = legacy; } else { id = 162; meta = legacy - 4; } return true; }
            if(rest == wood + "_leaves") { if(legacy <= 3) { id = 18; meta = legacy; } else { id = 161; meta = legacy - 4; } return true; }
            if(rest == wood + "_stairs") { id = legacy == 0 ? 53 : legacy == 1 ? 134 : legacy == 2 ? 135 : legacy == 3 ? 136 : legacy == 4 ? 163 : 164; meta = StairMeta(props); return true; }
            if(rest == wood + "_slab") { id = SlabDouble(props) ? 125 : 126; meta = legacy | SlabTop(props); return true; }
            if(rest == wood + "_fence") { id = 85; return true; }
            if(rest == wood + "_fence_gate") { id = 107; return true; }
            if(rest == wood + "_door") { id = 64; return true; }
            if(rest == wood + "_trapdoor") { id = 96; return true; }
            if(rest == wood + "_sapling" || rest == wood + "_propagule") { id = 6; meta = legacy & 7; return true; }
        }

        // stairs, slabs and walls of stone-like materials
        if(n.EndsWith("_stairs")) {
            meta = StairMeta(props);
            if(n.StartsWith("cobblestone") || n.StartsWith("stone_") && !n.StartsWith("stone_brick") || n.Contains("deepslate") || n.Contains("andesite") || n.Contains("diorite") || n.Contains("granite")) id = 67;
            else if(n.StartsWith("brick")) id = 108;
            else if(n.Contains("stone_brick")) id = 109;
            else if(n.Contains("nether_brick")) id = 114;
            else if(n.Contains("sandstone")) id = 128;
            else if(n.Contains("quartz") || n.Contains("purpur")) id = 156;
            else id = 109;
            return true;
        }
        if(n.EndsWith("_slab")) {
            int kind = n.Contains("sandstone") ? 1 : n.StartsWith("cobble") ? 3 : n.StartsWith("brick") ? 4 : n.Contains("stone_brick") ? 5 : n.Contains("nether_brick") ? 6 : n.Contains("quartz") || n.Contains("purpur") ? 7 : 0;
            if(SlabDouble(props)) { id = 43; meta = kind; } else { id = 44; meta = kind | SlabTop(props); }
            return true;
        }
        if(n.EndsWith("_wall")) { id = 139; return true; }

        // remaining families by keyword
        if(n.Contains("coral")) { id = 35; meta = n.Contains("brain") ? 6 : n.Contains("bubble") ? 10 : n.Contains("fire") ? 14 : n.Contains("horn") ? 4 : n.Contains("tube") ? 11 : 8; return true; }
        if(n.Contains("copper")) { id = 42; return true; }
        if(n.Contains("ore")) { id = 16; return true; }
        if(n.Contains("glass")) { id = 20; return true; }
        if(n.Contains("leaves")) { id = 18; return true; }
        if(n.Contains("brick")) { id = 45; return true; }
        if(n.Contains("planks")) { id = 5; return true; }
        if(n.Contains("log") || n.Contains("stem") || n.Contains("wood")) { id = 17; return true; }
        if(n.Contains("ice")) { id = 79; return true; }
        if(n.Contains("sand")) { id = 12; return true; }
        if(n.Contains("flower") || n.Contains("tulip") || n.Contains("bush") || n.Contains("roots") || n.Contains("sprouts") || n.Contains("vines") || n.Contains("fungus")) { id = 31; meta = 1; return true; }
        id = 1; meta = 0;                       // visible fallback: stone
        return true;
    }

    static void Split(int v, out int id, out int meta) { id = v >> 4; meta = v & 15; }

    static string Prop(IDictionary<string,string> p, string key) { string s; return p != null && p.TryGetValue(key, out s) ? s : null; }

    // Legacy stairs: 0 = east, 1 = west, 2 = south, 3 = north; +4 upside down.
    static int StairMeta(IDictionary<string,string> p) {
        string f = Prop(p, "facing"); int m = f == "east" ? 0 : f == "west" ? 1 : f == "south" ? 2 : 3;
        if(Prop(p, "half") == "top") m |= 4;
        return m;
    }
    static int SlabTop(IDictionary<string,string> p) { return Prop(p, "type") == "top" ? 8 : 0; }
    static bool SlabDouble(IDictionary<string,string> p) { return Prop(p, "type") == "double"; }
}
