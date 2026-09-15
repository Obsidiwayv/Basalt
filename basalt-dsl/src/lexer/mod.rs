pub mod common;

#[derive(Debug)]
pub enum LavaNodeType {
    StringType,
    ArritibuteStart,
    ArrayStart,
    ArrayEnd,
    FunctionStart,
    FunctionEnd,
    Keyword, // just assume everything is a keyword
}

pub struct LavaNode {
    pub value: String,
    pub node_type: LavaNodeType,
}
