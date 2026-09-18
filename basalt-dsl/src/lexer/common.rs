use std::iter::Peekable;

use basalt_common::object::StringCache;

use crate::lexer::{LavaNode, LavaNodeType};

pub struct LavaLexer {
    pub nodes: Vec<LavaNode>,
    pub lines: i32,
}

impl LavaLexer {
    pub fn new() -> LavaLexer {
        LavaLexer {
            nodes: Vec::new(),
            lines: 0,
        }
    }

    pub fn run(&mut self, file_content: Vec<char>) {
        let mut peekable_chars = file_content.iter().peekable();
        let mut word = StringCache::new();

        while let Some(c) = peekable_chars.next() {
            match c {
                ' ' | '\t' | '\n' => {
                    let word_copy = &word;

                    if c == &'\n' {
                        self.lines += 1;
                    }

                    if !(*word_copy).is_empty() {
                        self.nodes.push(LavaNode {
                            value: (*word_copy).to_string(),
                            node_type: LavaNodeType::Keyword,
                        });
                    }
                    word.clear();
                }
                '"' => self.parse_string(&mut peekable_chars),
                '(' | ')' | ',' => self.parse_functions(c, &mut word),
                '{' | '}' => self.handle_array_chars(c),
                '@' => {
                    self.nodes.push(LavaNode {
                        value: c.to_string(),
                        node_type: LavaNodeType::AttributeStart,
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

    fn parse_functions(&mut self, c: &char, word: &mut StringCache) {
        if c == &'(' {
            self.nodes.push(LavaNode {
                value: word.to_string(),
                node_type: LavaNodeType::FunctionHandle,
            });
            self.nodes.push(LavaNode {
                value: "(".to_string(),
                node_type: LavaNodeType::FunctionStart,
            });
            word.clear();
        }
        if c == &',' {
            self.push_function_keyword(word);
        }
        if c == &')' {
            self.push_function_keyword(word);
            self.nodes.push(LavaNode {
                value: ")".to_string(),
                node_type: LavaNodeType::FunctionEnd,
            });
        }
    }

    fn push_function_keyword(&mut self, word: &mut StringCache) {
        if word.is_empty() {
            return;
        }
        self.nodes.push(LavaNode {
            value: word.to_string(),
            node_type: LavaNodeType::FunctionKeyword,
        });
        word.clear();
    }

    fn handle_array_chars(&mut self, c: &char) {
        let array_bracket_type: LavaNodeType;

        if c == &'{' {
            array_bracket_type = LavaNodeType::ArrayStart;
        } else {
            array_bracket_type = LavaNodeType::ArrayEnd;
        }

        self.nodes.push(LavaNode {
            value: c.to_string(),
            node_type: array_bracket_type,
        });
    }
}
