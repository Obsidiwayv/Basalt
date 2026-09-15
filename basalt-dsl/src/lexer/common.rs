use std::iter::Peekable;

use basalt_common::object::StringCache;

use crate::lexer::{LavaNode, LavaNodeType};

pub struct LavaLexer {
    pub nodes: Vec<LavaNode>,
}

impl LavaLexer {
    pub fn new() -> LavaLexer {
        LavaLexer { nodes: Vec::new() }
    }

    pub fn run(&mut self, file_content: Vec<char>) {
        let mut current_line = 0;
        let mut peekable_chars = file_content.iter().peekable();
        let mut word = StringCache::new();

        while let Some(c) = peekable_chars.next() {
            match c {
                '\n' => {
                    current_line += 1;
                }
                ' ' | '\t' => {
                    let word_copy = &word;
                    if !(*word_copy).is_empty() {
                        self.nodes.push(LavaNode {
                            value: (*word_copy).to_string(),
                            node_type: LavaNodeType::Keyword,
                        });
                    }
                    word.clear();
                }
                '"' => self.parse_string(&mut peekable_chars),
                '{' | '}' => self.handle_array_chars(c),
                '@' => {
                    self.nodes.push(LavaNode {
                        value: c.to_string(),
                        node_type: LavaNodeType::ArritibuteStart,
                    });
                }
                &_ => word.push(c.to_string()),
            }
        }
    }

    fn parse_string(&mut self, it: &mut Peekable<std::slice::Iter<char>>) {
        let mut string_var: StringCache = StringCache::new();

        while let Some(&c_str) = it.next() {
            if c_str == '"' {
                self.nodes.push(LavaNode {
                    value: string_var.to_string(),
                    node_type: LavaNodeType::StringType,
                });
                it.next();
                break;
            }
            string_var.push(c_str.to_string());
        }
    }

    fn handle_array_chars(&mut self, c: &char) {
        if c == &'{' {
            self.nodes.push(LavaNode {
                value: c.to_string(),
                node_type: LavaNodeType::ArrayStart,
            });
        }
        if c == &'}' {
            self.nodes.push(LavaNode {
                value: c.to_string(),
                node_type: LavaNodeType::ArrayEnd,
            });
        }
    }
}
