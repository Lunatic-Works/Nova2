class_name BaseBlock extends RuntimeBlock

func __eval() -> void:
	push_error("Must override __eval in child")
	
func run() -> void:
	__eval()
