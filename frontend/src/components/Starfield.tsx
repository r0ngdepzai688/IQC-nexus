"use client";

import React, { useRef, useEffect } from "react";

export default function Starfield() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    let animationFrameId: number;
    let stars: Star[] = [];
    const numStars = 400; // Dense starfield for galaxy effect

    class Star {
      x: number;
      y: number;
      size: number;
      speed: number;
      opacity: number;
      opacitySpeed: number;

      constructor(w: number, h: number) {
        this.x = Math.random() * w;
        this.y = Math.random() * h;
        this.size = Math.random() * 1.5;
        this.speed = Math.random() * 0.05 + 0.01;
        this.opacity = Math.random();
        this.opacitySpeed = (Math.random() * 0.02) * (Math.random() > 0.5 ? 1 : -1);
      }

      update(h: number) {
        // Slowly drift upwards
        this.y -= this.speed;
        if (this.y < 0) {
          this.y = h;
          this.x = Math.random() * canvas!.width;
        }

        // Twinkle effect
        this.opacity += this.opacitySpeed;
        if (this.opacity <= 0.1 || this.opacity >= 1) {
          this.opacitySpeed *= -1;
        }
      }

      draw(ctx: CanvasRenderingContext2D) {
        ctx.beginPath();
        ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(255, 255, 255, ${this.opacity})`;
        ctx.fill();
      }
    }

    const resize = () => {
      const parent = canvas.parentElement;
      if (parent) {
        canvas.width = parent.clientWidth;
        canvas.height = parent.clientHeight;
        stars = [];
        for (let i = 0; i < numStars; i++) {
          stars.push(new Star(canvas.width, canvas.height));
        }
      }
    };

    window.addEventListener("resize", resize);
    resize();

    const render = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);

      for (let i = 0; i < stars.length; i++) {
        stars[i].update(canvas.height);
        stars[i].draw(ctx);
      }

      animationFrameId = requestAnimationFrame(render);
    };

    render();

    return () => {
      window.removeEventListener("resize", resize);
      cancelAnimationFrame(animationFrameId);
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      className="absolute inset-0 w-full h-full pointer-events-none z-0 opacity-60"
    />
  );
}
