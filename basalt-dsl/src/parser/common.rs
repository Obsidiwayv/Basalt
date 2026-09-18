use std::iter::Peekable;

use basalt_common::project::depot::ProjectNodes;

use crate::lexer::{LavaNode, LavaNodeType};

pub struct LavaParser {
    pub lexer_nodes: Vec<LavaNode>,
    pub nodes: ProjectNodes,
}

impl LavaParser {
    pub fn new(lexer_nodes: Vec<LavaNode>) -> LavaParser {
        LavaParser {
            lexer_nodes,
            nodes: ProjectNodes::new(), // init a default empty project node cache
        }
    }

    pub fn run(&self) {
        let mut it = self.lexer_nodes.iter().peekable();
        while let Some(token) = it.next() {
            self.parse_token(token, &mut it);
        }
    }

    fn parse_token(&self, token: &LavaNode, it: &mut Peekable<std::slice::Iter<LavaNode>>) {
        if token.node_type == LavaNodeType::FunctionHandle {}
    }
}
