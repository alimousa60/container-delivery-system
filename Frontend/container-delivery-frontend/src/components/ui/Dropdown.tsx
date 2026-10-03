import React, { useState, useRef, useEffect, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { clsx } from 'clsx';

interface DropdownItem {
  label: string;
  onClick: () => void;
  icon?: React.ReactNode;
  disabled?: boolean;
  danger?: boolean;
  divider?: boolean;
}

interface DropdownProps {
  trigger: React.ReactNode;
  items: DropdownItem[];
  align?: 'left' | 'right';
  offset?: number;
}

export const Dropdown: React.FC<DropdownProps> = ({ 
  trigger, 
  items, 
  align = 'right', 
  offset = 4 
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLDivElement>(null);

  const handleClickOutside = useCallback((event: MouseEvent) => {
    if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node) &&
        triggerRef.current && !triggerRef.current.contains(event.target as Node)) {
      setIsOpen(false);
    }
  }, []);

  useEffect(() => {
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [handleClickOutside]);

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Escape') {
      setIsOpen(false);
    }
  };

  const dropdownContent = isOpen ? (
    <div
      ref={dropdownRef}
      className={clsx(
        'dropdown',
        align === 'right' ? 'right-0' : 'left-0'
      )}
      style={{ marginTop: offset }}
      role="menu"
      onKeyDown={handleKeyDown}
    >
      {items.map((item, index) => (
        item.divider ? (
          <div key={`divider-${index}`} className="dropdown-divider" role="separator" />
        ) : (
          <button
            key={index}
            onClick={() => {
              if (!item.disabled) {
                item.onClick();
                setIsOpen(false);
              }
            }}
            disabled={item.disabled}
            className={clsx(
              'dropdown-item w-full text-left',
              item.danger && 'text-danger-600 dark:text-danger-400',
              item.disabled && 'opacity-50 cursor-not-allowed'
            )}
            role="menuitem"
            tabIndex={-1}
          >
            {item.icon && <span className="w-5 h-5 flex-shrink-0">{item.icon}</span>}
            <span className="flex-1">{item.label}</span>
          </button>
        )
      ))}
    </div>
  ) : null;

  return (
    <div className="relative inline-block" ref={triggerRef}>
      <div onClick={() => setIsOpen(!isOpen)} className="inline-block">
        {trigger}
      </div>
      {createPortal(dropdownContent, document.body)}
    </div>
  );
};