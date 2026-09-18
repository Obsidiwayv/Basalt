pub mod common;

#[derive(Debug, PartialEq, Eq)]
pub enum LavaNodeType {
    StringType,
    AttributeStart,
    ArrayStart,
    ArrayEnd,
    FunctionStart,
    FunctionEnd,
    FunctionHandle,
    Keyword, // just assume everything is a keyword
    FunctionKeyword,
}

#[derive(Debug)]
pub struct LavaNode {
    pub value: String,
    pub node_type: LavaNodeType,
}
