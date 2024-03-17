class_name ConditionBlock extends BaseBlock

func __eval() -> bool:
    push_error("Must override __eval in child")
    return false

func run() -> bool:
    return __eval()
