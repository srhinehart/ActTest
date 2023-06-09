echo(version=version());
use <mcad\boxes.scad>

// Limit Switch Mounting Bracket

// Slice conditions:
// .STL file generated by OpenSCAD 2015.3.0
// .STL file sliced with Cura 15.04.6: 0.2mm layer height, 0.4mm nozzle
// 50% infill, 45mm/s speed, 98% flow rate

c_res = 50;
c_rad = 2;

thickness = 4;
hole_diam = 4.4;
hole_sep = 12;

sw_hole_top = 30;
sw_hole_sep = 9.5;
sw_hole_diam = 1.9;

height = 35;
slot_z_center = 25;

width = 22;
fudge = 0.1;

offset = (0.5 * width) - c_rad;

module sw_hole(z_pos)
{
    rotate([0, -90, 0])
    union()
    {
        translate([z_pos, 0 , -width * 0.5 -fudge*.5])
            cylinder(d=sw_hole_diam, h=thickness + fudge, center = false, $fn=c_res);
    }
}

module cutouts() {
    // mounting screw holes
    translate([0,0,-0.5 * fudge])
        cylinder(h=thickness + fudge, d=hole_diam, center=false, $fn=c_res);
    translate([0 - hole_sep,0,-0.5 * fudge])
        cylinder(h=thickness + fudge, d=hole_diam, center=false, $fn=c_res);
    
    // switch holes
    sw_hole(sw_hole_top);
    sw_hole(sw_hole_top - sw_hole_sep);
}

module sidesupport(y_sign)
{
    hull()
    {
        translate([offset, y_sign * offset, c_rad])
            sphere(r=c_rad, $fn=c_res);
        translate([-offset, y_sign * offset, c_rad])
            sphere(r=c_rad, $fn=c_res);
        translate([offset, y_sign * offset, height - c_rad])
            sphere(r=c_rad, $fn=c_res);
    }
}

module sideplate()
{
    hull()
    {
        translate([offset, offset, c_rad])
            sphere(r=c_rad, $fn=c_res);
        translate([offset, -offset, c_rad])
            sphere(r=c_rad, $fn=c_res);
        translate([offset, offset, height - c_rad])
            sphere(r=c_rad, $fn=c_res);
        translate([offset, -offset, height - c_rad])
            sphere(r=c_rad, $fn=c_res);
    }
}

module bottomplate()
{
    hull() {
        translate([offset, offset, c_rad])
            sphere(r=c_rad, $fn=c_res);
        translate([offset, -offset, c_rad])
            sphere(r=c_rad, $fn=c_res);    
        translate([-2 * offset, -offset, c_rad])
            sphere(r=c_rad, $fn=c_res);
        translate([-2 * offset, offset, c_rad])
            sphere(r=c_rad, $fn=c_res);    
    }
}


module block() {
    difference()
    {
        union()
        {
            sidesupport(-1);
            sidesupport(+1);
            bottomplate();
            sideplate();
        }
        cutouts();
    }
}
        


color("red")
        //cutouts();
        block();

