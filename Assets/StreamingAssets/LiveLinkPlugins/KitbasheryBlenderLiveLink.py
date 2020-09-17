bl_info = {
    "name": "Kitbashery Live Link",
    "description": "Live link between Blender and Kitbashery",
    "author": "Kitbashery",
    "version": (1, 0, 0),
    "blender": (2, 83, 0),
    "location": "",
    "category": "Import-Export",
    "wiki_url": "Kitbashery.com"
}

import bpy, sys, socket, json

host, port = 'localhost', 26738

class LiveLink():
    bl_idname = "live.link"

    def execute(self, context):
        client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client_socket.connect((host, port))
        while 1:
            data = client_socket.recv(512)
            if len(data) > 0:
                try:
                    #Deselect all selections:
                    context.selected_objects = null
                    #Go into editmode:
                    #context.scene.mode = 
                
                    #import mesh from json:
                    jsonData = json.Loads(data.decode())
                    print(jsonData)
                    print(jsonData["meshPath"])
                    #https://docs.blender.org/api/current/bpy.ops.import_scene.html
                    bpy.ops.import_scene.obj(filepath= jsonData["meshPath"], filter_glob="*.obj;", use_edges=True, use_smooth_groups=True, use_split_objects=False, use_split_groups=False, use_groups_as_vgroups=False, use_image_search=False, split_mode='ON', global_clight_size=0.0, axis_forward='-Z', axis_up='Y')
                    
                    #Get selected mesh (should be the import)
                    #Convert tris of imported mesh faces to quads then get seams from uv islands  
                    #Go to object mode                    
                except:
                    print("no data?")
                    #client_socket.close()
                    break;
                    
def register():
    bpy.utils.register_class(LiveLink)

def unregister():
    bpy.utils.unregister_class(LiveLink)

if __name__ == "__main__":
    register()