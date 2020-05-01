// Available inputs
// mouse (vec4) : x,y => position, z => mouse 1 down, z => mouse 2 down
// resolution (vec2) : x,y => pixel size of the render window
// time (float) : total time in seconds since start
// deltaTime (float) : time in seconds since last frame
// frame (int) : current frame

void main()
{
    float x = gl_FragCoord.x / resolution.x;
    float y = gl_FragCoord.y / resolution.y;
    out_Color = vec4(0,x,y,1);
}