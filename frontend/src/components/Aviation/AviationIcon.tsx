import { Plane, PlaneTakeoff, PlaneLanding, Package, Shield, Circle } from 'lucide-react';

interface AviationIconProps {
  type: 'Commercial' | 'Cargo' | 'Private' | 'Charter' | 'Military' | 'Helicopter' | 'Other';
  size?: number;
  className?: string;
}

export const AviationIcon: React.FC<AviationIconProps> = ({ type, size = 24, className = '' }) => {
  const iconProps = {
    size,
    className,
    strokeWidth: 2,
  };

  const icons: Record<string, JSX.Element> = {
    Commercial: <Plane {...iconProps} />,
    Cargo: <Package {...iconProps} />,
    Private: <PlaneTakeoff {...iconProps} />,
    Charter: <Plane {...iconProps} />,
    Military: <Shield {...iconProps} />,
    Helicopter: <Circle {...iconProps} />, // Using Circle as Helicopter icon doesn't exist in lucide-react
    Other: <PlaneLanding {...iconProps} />,
  };

  return icons[type] || icons.Other;
};

export default AviationIcon;
