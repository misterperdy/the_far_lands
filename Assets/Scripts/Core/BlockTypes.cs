//enum data type to pair block types with actual byte IDs ( byte max value is 255 - performance optimized )

public enum BlockType : byte {
    Air = 0,
    Dirt = 1,
    Stone = 2,
    Grass = 3,
    Planks = 4,
    Bricks = 5,
    StoneBricks = 6,
    Sand = 7,
    Glass = 8,
    Bedrock = 9,
    TallGrass = 10,
    BrownMushroom = 11,
    RedMushroom = 12,
    Leaves = 13,
    Wood = 14
}

//on enum you can explicitly write the actual value of the item (default is int, here we changed to byte) , unity automatically does this but its good to write it now explicit
// so that when we add new blocks already created chunks don't corrupt/change blocks and to visualize them better