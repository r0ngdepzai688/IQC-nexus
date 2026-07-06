// Geometric Point Samplers for Constellations
export const sampleLine = (x1: number, y1: number, x2: number, y2: number, numPoints: number) => {
    const points = [];
    for(let i=0; i<numPoints; i++) {
        const t = numPoints === 1 ? 0.5 : i / (numPoints - 1);
        points.push({ x: x1 + (x2 - x1) * t, y: y1 + (y2 - y1) * t });
    }
    return points;
};

export const sampleCircle = (cx: number, cy: number, r: number, numPoints: number, startAngle=0, endAngle=Math.PI*2) => {
    const points = [];
    for(let i=0; i<numPoints; i++) {
        const t = i / numPoints;
        const angle = startAngle + (endAngle - startAngle) * t;
        points.push({ x: cx + Math.cos(angle) * r, y: cy + Math.sin(angle) * r });
    }
    return points;
};

export const sampleRoundedRect = (cx: number, cy: number, w: number, h: number, radius: number, totalPoints: number) => {
    const points: {x: number, y: number}[] = [];
    // Perimeter calculation
    const straightW = w - 2*radius;
    const straightH = h - 2*radius;
    const perimeter = 2*straightW + 2*straightH + 2*Math.PI*radius;
    
    // Points per segment
    const ptTop = Math.round((straightW / perimeter) * totalPoints);
    const ptBottom = ptTop;
    const ptLeft = Math.round((straightH / perimeter) * totalPoints);
    const ptRight = ptLeft;
    const ptCorner = Math.round(((Math.PI*radius/2) / perimeter) * totalPoints);

    const left = cx - w/2;
    const right = cx + w/2;
    const top = cy - h/2;
    const bottom = cy + h/2;

    // Top Edge
    points.push(...sampleLine(left+radius, top, right-radius, top, ptTop));
    // Top Right Corner
    points.push(...sampleCircle(right-radius, top+radius, radius, ptCorner, -Math.PI/2, 0));
    // Right Edge
    points.push(...sampleLine(right, top+radius, right, bottom-radius, ptRight));
    // Bottom Right Corner
    points.push(...sampleCircle(right-radius, bottom-radius, radius, ptCorner, 0, Math.PI/2));
    // Bottom Edge
    points.push(...sampleLine(right-radius, bottom, left+radius, bottom, ptBottom));
    // Bottom Left Corner
    points.push(...sampleCircle(left+radius, bottom-radius, radius, ptCorner, Math.PI/2, Math.PI));
    // Left Edge
    points.push(...sampleLine(left, bottom-radius, left, top+radius, ptLeft));
    // Top Left Corner
    points.push(...sampleCircle(left+radius, top+radius, radius, ptCorner, Math.PI, Math.PI*1.5));

    return points;
};
