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
    if obj is String:
        return o[obj]
    return obj

func _get_index(arr: Array, index: int, default=null):
    return arr[index] if index < len(arr) else default

func move(obj, coord) -> void:
    if !(coord is Vector3):
        var z = _get_index(coord, 2, obj.position.z)
        coord = Vector3(coord[0], coord[1], z)
    obj.position = coord

func tint(obj, color) -> void:
    if !(color is Color):
        var a = _get_index(color, 3, 1)
        color = Color(color[0], color[1], color[2], a)
    obj.modulate = color

func show(obj, image_path, coord=null, color=null) -> void:
    obj = _get_obj(obj)

    var path = c.resource_root
    if obj.has_meta("folder"):
        path += obj.get_meta("folder") + "/"
    path += image_path + ".png"

    if coord != null:
        move(obj, coord)
    if color != null:
        tint(obj, color)

    obj.texture = load(path)
    obj.visible = true

func hide(obj) -> void:
    obj = _get_obj(obj)
    obj.visible = false
