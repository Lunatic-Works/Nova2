class_name RuntimeBlock extends BaseBlock

var nova_controller: Node:
    get:
        var tree = Engine.get_main_loop() as SceneTree
        return tree.root.get_node("NovaController")

var o: Dictionary:
    get:
        return nova_controller.ObjectManager.Objects

var c: Dictionary:
    get:
        return nova_controller.ObjectManager.Constants

func _get_obj(obj):
    if typeof(obj) == TYPE_STRING:
        return o[obj]
    return obj

func show(obj, image_path) -> void:
    obj = _get_obj(obj)
    var path = c.resource_root
    if (obj.has_meta("folder")):
        path += obj.get_meta("folder") + "/"
    path += image_path + ".png"

    obj.texture = load(path)
    obj.visible = true

func hide(obj) -> void:
    obj = _get_obj(obj)
    obj.visible = false
