pub struct LavaStringNode {
    key: &'static str,
    value: &'static str,
}

impl LavaStringNode {
    pub fn get_boolean(&self) -> bool {
        match self.value {
            "enable" => true,
            "disable" => false,
            &_ => panic!("Node: {} Invalid boolean option", self.key),
        }
    }
}
