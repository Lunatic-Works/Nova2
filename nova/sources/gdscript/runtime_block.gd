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
    return arr[index] if index < len(arr) and arr[index] != null else default

func _get_vec3(input, default: Vector3, single_default=null) -> Vector3:
    if input is Vector3:
        return input
    elif (input is int or input is float) and single_default != null:
        return single_default.call(input)
    elif input != null:
        var x = _get_index(input, 0, default.x)
        var y = _get_index(input, 1, default.y)
        var z = _get_index(input, 2, default.z)
        return Vector3(x, y, z)
    else:
        return default

func move(obj, coord, scale=null, angle=null) -> void:
    obj = _get_obj(obj)
    if coord != null and not coord is Vector3:
        if scale == null:
            scale = _get_vec3(_get_index(coord, 2, null), obj.scale,
                func(s): return Vector3(s, s, 1))
        if angle == null:
            angle = _get_vec3(_get_index(coord, 4, null), obj.rotation_degrees,
                func(s): return Vector3(0, 0, s))
        coord = _get_vec3(coord, obj.position)

    if coord != null:
        obj.position = coord
    if scale != null:
        obj.scale = scale
    if angle != null:
        obj.rotation_degrees = angle

func tint(obj, color) -> void:
    obj = _get_obj(obj)
    if not color is Color:
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
