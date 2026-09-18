pub struct LavaArray {
    pub name: String,
    pub children: Vec<String>,
}

pub struct LavaAttribute {
    pub name: String,
    pub value: String,
}

pub struct LavaFunction {
    pub handle: String,
    pub params: Vec<String>,
}

impl LavaAttribute {
    pub fn new(name: String, value: String) -> LavaAttribute {
        LavaAttribute { name, value }
    }

    pub fn as_int(self) -> i32 {
        self.value
            .parse::<i32>()
            .expect("LavaAttribute.value was not an integer")
    }
}
