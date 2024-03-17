class_name LazyBlock extends BaseBlock

var nova_controller: Node:
    get:
        var tree = Engine.get_main_loop() as SceneTree
        return tree.root.get_node("NovaController")

func __eval() -> void:
    pass

func run() -> void:
    __eval()
