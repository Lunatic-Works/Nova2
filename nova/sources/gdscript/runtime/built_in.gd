class_name BuiltIn

static var _nova: Node

static func _static_init():
	var tree: SceneTree = Engine.get_main_loop()
	_nova = tree.root.get_node("NovaController")

#@export
static var o: Dictionary:
	get:
		return _nova.ObjectManager.Objects

#@export
static var c: Dictionary:
	get:
		return _nova.ObjectManager.Constants

static func _get_obj(obj: Variant) -> Variant:
	if obj is String:
		return o[obj]
	return obj

static func _get_index(arr: Array, index: int, default=null):
	return arr[index] if index < len(arr) and arr[index] != null else default

static func _get_vec3(input, default: Vector3, single_default=null) -> Vector3:
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
