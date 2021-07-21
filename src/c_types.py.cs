
using ABC = abc.ABC;

using Sequence = typing.Sequence;

public static class c_types {
    
    public class CType
        : ABC {
        
        public object size {
            get {
            }
        }
    }
    
    public class VoidType
        : CType {
        
        public object size {
            get {
                return 0;
            }
        }
    }
    
    public class IntType
        : CType {
        
        public object signed;
        
        public object width;
        
        public IntType(Func<object> width = @int, Func<object> signed = @bool) {
            this.width = width;
            this.signed = signed;
        }
        
        public object size {
            get {
                return this.width;
            }
        }
    }
    
    public class FloatType
        : CType {
        
        public object width;
        
        public FloatType(Func<object> width = @int) {
            this.width = width;
        }
        
        public object size {
            get {
                return this.width;
            }
        }
    }
    
    public class CharType
        : CType {
        
        public object width;
        
        public CharType(Func<object> width = @int) {
            this.width = width;
        }
        
        public object size {
            get {
                return this.width;
            }
        }
    }
    
    public class FieldType
        : CType {
        
        public object ctype;
        
        public object offset;
        
        public FieldType(CType ctype = CType, Func<object> offset = @int) {
            this.ctype = ctype;
            this.offset = offset;
        }
        
        public object size {
            get {
                return this.ctype.size;
            }
        }
    }
    
    public class StructType
        : CType {
        
        public object fields;
        
        public StructType(object fields = Sequence[FieldType]) {
            this.fields = fields;
        }
        
        public object size {
            get {
                throw new NotImplemented();
            }
        }
    }
    
    public class ArrayType
        : CType {
        
        public object length;
        
        public object member_type;
        
        public ArrayType(CType member_type = CType, Func<object> length = @int) {
            this.member_type = member_type;
            this.length = length;
        }
        
        public object size {
            get {
                return this.member_type.size * this.length;
            }
        }
    }
    
    public class PointerType
        : CType {
        
        public object target_type;
        
        public PointerType(CType target_type = CType) {
            this.target_type = target_type;
        }
        
        public object size {
            get {
                throw new NotImplemented();
            }
        }
    }
    
    public class FunctionType
        : CType {
        
        public object params;
        
        public object return_type;
        
        public FunctionType(CType return_type = CType, object params = Sequence[CType]) {
            this.return_type = return_type;
            this.params = params;
        }
        
        public object size {
            get {
                throw new NotImplemented();
            }
        }
    }
    
    // Retypd - machine code type inference
    // Copyright (C) 2021 GrammaTech, Inc.
    //
    // This program is free software: you can redistribute it and/or modify
    // it under the terms of the GNU General Public License as published by
    // the Free Software Foundation, either version 3 of the License, or
    // (at your option) any later version.
    //
    // This program is distributed in the hope that it will be useful,
    // but WITHOUT ANY WARRANTY; without even the implied warranty of
    // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    // GNU General Public License for more details.
    //
    // You should have received a copy of the GNU General Public License
    // along with this program.  If not, see <https://www.gnu.org/licenses/>.
    //
    // This project is sponsored by the Office of Naval Research, One Liberty
    // Center, 875 N. Randolph Street, Arlington, VA 22203 under contract #
    // N68335-17-C-0700.  The content of the information does not necessarily
    // reflect the position or policy of the Government and no official
    // endorsement should be inferred.
    public static void UnionType(object CType) {
        Func<object, object, object> @__init__ = (self,ctypes) => {
            this.ctypes = ctypes;
        };
        Func<object, object> size = self => {
            return max(map(t => t.size, this.ctypes));
        };
    }
}
